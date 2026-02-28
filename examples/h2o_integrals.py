#!/usr/bin/env python3
"""
Generate molecular integrals for H₂O using PySCF.

This script runs a Hartree-Fock calculation on water in a minimal basis (STO-3G),
extracts one- and two-electron integrals in the molecular orbital basis, and
exports them as JSON for consumption by FockMap (F#).

Usage:
    python h2o_integrals.py                         # default geometry (104.52°)
    python h2o_integrals.py --angle 90              # custom H-O-H angle
    python h2o_integrals.py --scan 80 130 11         # angle scan from 80° to 130° in 11 steps

Output:
    h2o_integrals.json      — integral data + metadata for one geometry
    h2o_scan.json           — array of integral data for angle scan
"""

import argparse
import json
import math
import sys

import numpy as np
from pyscf import ao2mo, gto, scf


def build_water(angle_deg: float, r_oh: float = 0.9572) -> gto.Mole:
    """Build H₂O molecule with given H-O-H angle and O-H distance (in Å)."""
    angle_rad = math.radians(angle_deg)
    # O at origin, H atoms symmetric about the z-axis in the yz-plane
    hy = r_oh * math.sin(angle_rad / 2)
    hz = r_oh * math.cos(angle_rad / 2)
    mol = gto.M(
        atom=f'O 0.0 0.0 0.0; H 0.0 {hy:.6f} {hz:.6f}; H 0.0 {-hy:.6f} {hz:.6f}',
        basis='sto-3g',
        symmetry=False,
        verbose=0,
    )
    return mol


def compute_integrals(mol: gto.Mole, freeze_core: bool = True):
    """
    Run RHF and extract MO integrals.

    Returns:
        dict with keys:
            n_electrons, n_orbitals, n_spin_orbitals, n_frozen,
            hf_energy, nuclear_repulsion,
            one_body (dict of "pq" -> value),
            two_body (dict of "pqrs" -> value),
            geometry, basis
    """
    mf = scf.RHF(mol).run()

    mo_coeff = mf.mo_coeff
    n_ao = mol.nao_nr()
    n_mo = mo_coeff.shape[1]

    # One-electron integrals: kinetic + nuclear attraction
    h1_ao = mol.intor('int1e_kin') + mol.intor('int1e_nuc')
    h1_mo = mo_coeff.T @ h1_ao @ mo_coeff

    # Two-electron integrals in MO basis (chemist's notation: (pq|rs))
    h2_mo = ao2mo.full(mol, mo_coeff)
    # Restore to 4-index array; ao2mo returns a compressed form
    h2_mo = ao2mo.restore(1, h2_mo, n_mo)  # shape (n_mo, n_mo, n_mo, n_mo)

    # Determine active space
    n_frozen = 0
    start = 0
    if freeze_core:
        # Freeze oxygen 1s orbital (lowest energy MO)
        n_frozen = 1
        start = 1

    n_active = n_mo - n_frozen
    n_spin_orbitals = 2 * n_active

    # Extract active-space integrals
    h1_active = h1_mo[start:, start:]
    h2_active = h2_mo[start:, start:, start:, start:]

    # Core energy contribution from frozen orbitals
    core_energy = 0.0
    if freeze_core:
        # One-electron part
        core_energy += 2.0 * h1_mo[0, 0]
        # Two-electron part (Coulomb - Exchange)
        for i in range(start, n_mo):
            pass  # We'll add it to nuclear repulsion as effective core
        # Simpler: frozen core Fock contribution
        core_energy += 2.0 * h2_mo[0, 0, 0, 0] - h2_mo[0, 0, 0, 0]  # J - K for single frozen orbital
        # Actually, for a proper frozen-core treatment:
        core_energy = 2.0 * h1_mo[0, 0]
        for j in range(start, n_mo):
            core_energy += 2.0 * h2_mo[0, 0, j, j] - h2_mo[0, j, j, 0]
        # Wait — that's modifying active integrals. Let me do this properly.

    # Actually, let's do frozen core properly:
    # The effective one-body integrals for the active space include
    # contributions from the frozen core electrons.
    if freeze_core:
        core_energy = 0.0
        # Core one-electron energy
        for i in range(n_frozen):
            core_energy += 2.0 * h1_mo[i, i]  # 2 for spin
        # Core two-electron energy
        for i in range(n_frozen):
            for j in range(n_frozen):
                core_energy += 2.0 * h2_mo[i, i, j, j] - h2_mo[i, j, j, i]

        # Effective one-electron integrals (add frozen-core Fock contribution)
        h1_eff = h1_active.copy()
        for p in range(n_active):
            for q in range(n_active):
                for i in range(n_frozen):
                    pp = p + start
                    qq = q + start
                    h1_eff[p, q] += (2.0 * h2_mo[pp, qq, i, i] - h2_mo[pp, i, i, qq])
        h1_active = h1_eff
    else:
        core_energy = 0.0

    # Build spin-orbital integrals
    # Spatial orbital p -> spin orbitals 2p (alpha), 2p+1 (beta)
    # One-body: h_{2p,2q} = h_{2p+1,2q+1} = h1[p,q], cross-spin = 0
    # Two-body: antisymmetrized <pq||rs> = <pq|rs> - <pq|sr>
    #   in physicist's notation: <pq|rs> = (pr|qs) in chemist's notation

    threshold = 1e-12

    # Build spin-orbital integrals with keys that match FockMap's
    # sprintf "%u,%u" / "%u,%u,%u,%u" convention (comma-separated).
    # Spatial orbital p → spin-orbitals 2p (α) and 2p+1 (β).
    one_body = {}
    for p in range(n_active):
        for q in range(n_active):
            val = h1_active[p, q]
            if abs(val) > threshold:
                # α-α and β-β (cross-spin one-body vanishes)
                one_body[f"{2*p},{2*q}"] = val
                one_body[f"{2*p+1},{2*q+1}"] = val

    two_body = {}
    for p in range(n_active):
        for q in range(n_active):
            for r in range(n_active):
                for s in range(n_active):
                    # Chemist's notation (pq|rs): integral over
                    # φ_p*(1) φ_q(1) (1/r₁₂) φ_r*(2) φ_s(2)
                    val = h2_active[p, q, r, s]
                    if abs(val) > threshold:
                        sp, sq, sr, ss = 2*p, 2*q, 2*r, 2*s
                        # αα|αα and ββ|ββ
                        two_body[f"{sp},{sq},{sr},{ss}"] = val
                        two_body[f"{sp+1},{sq+1},{sr+1},{ss+1}"] = val
                        # αα|ββ and ββ|αα
                        two_body[f"{sp},{sq},{sr+1},{ss+1}"] = val
                        two_body[f"{sp+1},{sq+1},{sr},{ss}"] = val

    result = {
        "molecule": "H2O",
        "basis": "STO-3G",
        "geometry": {
            "angle_deg": round(float(mol.atom_coords()[1][1]), 6) and float(
                math.degrees(2 * math.asin(
                    np.linalg.norm(mol.atom_coords()[1][:2] - mol.atom_coords()[0][:2])
                    / np.linalg.norm(mol.atom_coords()[1] - mol.atom_coords()[0])
                ))
            ) if len(mol.atom_coords()) > 1 else 0.0,
            "r_oh_angstrom": 0.9572,
            "atoms": mol.atom,
        },
        "n_electrons": mol.nelectron,
        "n_spatial_orbitals": n_active,
        "n_spin_orbitals": n_spin_orbitals,
        "n_frozen_core": n_frozen,
        "hf_energy": float(mf.e_tot),
        "nuclear_repulsion": float(mol.energy_nuc()),
        "core_energy": float(core_energy),
        "one_body": {k: float(v) for k, v in one_body.items()},
        "two_body": {k: float(v) for k, v in two_body.items()},
    }

    return result


def main():
    parser = argparse.ArgumentParser(description="Generate H₂O molecular integrals")
    parser.add_argument("--angle", type=float, default=104.52,
                        help="H-O-H bond angle in degrees (default: 104.52)")
    parser.add_argument("--r-oh", type=float, default=0.9572,
                        help="O-H bond length in Å (default: 0.9572)")
    parser.add_argument("--no-freeze-core", action="store_true",
                        help="Don't freeze the oxygen 1s core orbital")
    parser.add_argument("--scan", nargs=3, metavar=("START", "END", "STEPS"),
                        type=float, help="Bond angle scan: START END STEPS")
    parser.add_argument("--output", type=str, default=None,
                        help="Output JSON file (default: auto)")

    args = parser.parse_args()
    freeze_core = not args.no_freeze_core

    if args.scan:
        start, end, steps = args.scan
        steps = int(steps)
        angles = np.linspace(start, end, steps)
        results = []
        for angle in angles:
            print(f"  Computing angle = {angle:.1f}°...", file=sys.stderr)
            mol = build_water(angle, args.r_oh)
            data = compute_integrals(mol, freeze_core)
            data["geometry"]["angle_deg"] = round(float(angle), 2)
            results.append(data)

        output = args.output or "h2o_scan.json"
        with open(output, 'w') as f:
            json.dump(results, f, indent=2)
        print(f"Wrote {len(results)} geometries to {output}", file=sys.stderr)

    else:
        mol = build_water(args.angle, args.r_oh)
        data = compute_integrals(mol, freeze_core)
        data["geometry"]["angle_deg"] = args.angle

        output = args.output or "h2o_integrals.json"
        with open(output, 'w') as f:
            json.dump(data, f, indent=2)

        print(f"H₂O ({args.angle:.1f}°, {args.r_oh:.4f} Å, {'frozen core' if freeze_core else 'full'})",
              file=sys.stderr)
        print(f"  Spin-orbitals: {data['n_spin_orbitals']}", file=sys.stderr)
        print(f"  HF energy:     {data['hf_energy']:.10f} Ha", file=sys.stderr)
        print(f"  Nuc. repul.:   {data['nuclear_repulsion']:.10f} Ha", file=sys.stderr)
        print(f"  One-body terms: {len(data['one_body'])}", file=sys.stderr)
        print(f"  Two-body terms: {len(data['two_body'])}", file=sys.stderr)
        print(f"  Wrote: {output}", file=sys.stderr)


if __name__ == "__main__":
    main()
