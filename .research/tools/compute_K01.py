"""
Compute the exchange integral K₀₁ = (01|01)_chemist for H₂/STO-3G
at R = 0.7414 Å (R = 1.4011 bohr) to full double precision.

STO-3G for hydrogen: 3 Gaussian primitives fitting a Slater 1s orbital (ζ=1.24).
The standard STO-3G coefficients for 1S (from Hehre, Stewart, Pople 1969):

  Exponents (α):  3.42525091, 0.62391373, 0.16885540
  Coefficients (d): 0.15432897, 0.53532814, 0.44463454

These coefficients are for NORMALIZED primitives:
  g(r; α) = (2α/π)^(3/4) * exp(-α r²)

The molecular orbitals for H₂ are:
  σ_g = (s_A + s_B) / sqrt(2 + 2*S_AB)
  σ_u = (s_A - s_B) / sqrt(2 - 2*S_AB)

where s_A and s_B are the 1s AOs centered on atoms A and B.

The exchange integral in chemist notation is:
  K₀₁ = (σ_g σ_u | σ_g σ_u) 
       = ∫∫ σ_g(r₁) σ_u(r₁) (1/r₁₂) σ_g(r₂) σ_u(r₂) dr₁ dr₂

Using Mulliken notation: (12|12) where 1=σ_g, 2=σ_u
"""

import numpy as np
from math import pi, sqrt, exp, erf
import itertools

# ══════════════════════════════════════════════════════════
#  STO-3G basis parameters for hydrogen (ζ = 1.24)
# ══════════════════════════════════════════════════════════

# Raw STO-3G exponents and contraction coefficients (for normalized primitives)
# From Hehre, Stewart, Pople, J. Chem. Phys. 51, 2657 (1969)
alpha_raw = np.array([3.42525091, 0.62391373, 0.16885540])
d_raw = np.array([0.15432897, 0.53532814, 0.44463454])

# Scale exponents by ζ² 
# NOTE: For hydrogen in STO-3G, the standard ζ = 1.24 for an isolated atom,
# but molecular calculations typically use ζ = 1.0 (the exponents are already
# optimized for the molecular context). Let's try both.
zeta = 1.0  # Try ζ = 1.0 first (standard for most molecular codes)
alpha = alpha_raw * zeta**2

# Normalization of each primitive: N = (2α/π)^(3/4)
def norm(a):
    return (2.0 * a / pi) ** 0.75

# Contracted coefficients (including primitive normalization)
coeff = d_raw * np.array([norm(a) for a in alpha])

n_prim = len(alpha)

# Atom positions along z-axis
# Try multiple bond length conventions to find which matches our integrals
R_angstrom = 0.7414
# PySCF uses BOHR = 0.52917721092 Å
# But our V_nn doesn't match with that. Let's find what Bohr constant gives our V_nn.
# V_nn = Z_A * Z_B / R_bohr = 1/R_bohr
# R_bohr = R_angstrom / BOHR
# V_nn = BOHR / R_angstrom = BOHR / 0.7414
# So BOHR = V_nn * R_angstrom = 0.7151043390810812 * 0.7414

bohr_from_Vnn = 0.7151043390810812 * R_angstrom
print(f"Bohr from V_nn: {bohr_from_Vnn:.14f} Å")
print(f"PySCF Bohr:     0.52917721092000 Å")
print(f"NIST 2014 Bohr: 0.52917721067000 Å")

# Use the Bohr constant that matches our nuclear repulsion exactly
R_bohr = R_angstrom / bohr_from_Vnn

print(f"Bond length: R = {R_angstrom} Å = {R_bohr:.10f} bohr")

# Atom A at origin, atom B at (0, 0, R)
A = np.array([0.0, 0.0, 0.0])
B = np.array([0.0, 0.0, R_bohr])

# ══════════════════════════════════════════════════════════
#  Gaussian integral formulas
# ══════════════════════════════════════════════════════════

def overlap_1d(a, b, Ra, Rb):
    """Overlap integral between two s-type Gaussians."""
    gamma = a + b
    Rp = (a * Ra + b * Rb) / gamma
    return (pi / gamma)**1.5 * exp(-a * b / gamma * np.sum((Ra - Rb)**2))

def boys_F0(T):
    """Boys function F₀(T) = ∫₀¹ exp(-T t²) dt"""
    if T < 1e-15:
        return 1.0
    return 0.5 * sqrt(pi / T) * erf(sqrt(T))

def eri_ssss(a, b, c, d, Ra, Rb, Rc, Rd):
    """
    Electron repulsion integral (ab|cd) over four s-type Gaussian primitives:
    (ab|cd) = ∫∫ g_a(r₁-Ra) g_b(r₁-Rb) (1/r₁₂) g_c(r₂-Rc) g_d(r₂-Rd) dr₁ dr₂
    
    where g_a(r) = exp(-a |r|²) (unnormalized primitive)
    """
    p = a + b
    q = c + d
    Rp = (a * Ra + b * Rb) / p
    Rq = (c * Rc + d * Rd) / q
    
    RPQ2 = np.sum((Rp - Rq)**2)
    RAB2 = np.sum((Ra - Rb)**2)
    RCD2 = np.sum((Rc - Rd)**2)
    
    alpha_pq = p * q / (p + q)
    T = alpha_pq * RPQ2
    
    prefactor = 2.0 * pi**2.5 / (p * q * sqrt(p + q))
    prefactor *= exp(-a * b / p * RAB2)
    prefactor *= exp(-c * d / q * RCD2)
    
    return prefactor * boys_F0(T)

# ══════════════════════════════════════════════════════════
#  Compute AO integrals
# ══════════════════════════════════════════════════════════

# AO basis: s_A (centered on A) and s_B (centered on B)
# Each is a contracted Gaussian with n_prim primitives

def ao_eri(centers):
    """
    Compute (μν|λσ) where each index specifies a center (A or B).
    centers = (C1, C2, C3, C4) - the center for each AO.
    """
    result = 0.0
    for i in range(n_prim):
        for j in range(n_prim):
            for k in range(n_prim):
                for l in range(n_prim):
                    result += (coeff[i] * coeff[j] * coeff[k] * coeff[l] *
                              eri_ssss(alpha[i], alpha[j], alpha[k], alpha[l],
                                      centers[0], centers[1], centers[2], centers[3]))
    return result

# Compute all unique AO ERIs
# Using notation: A=center A, B=center B
# (AA|AA), (BB|BB), (AA|BB), (AB|AB), (AB|BA), etc.

print("\n═══════════════════════════════════════════════════════")
print(" AO two-electron integrals (chemist notation)")
print("═══════════════════════════════════════════════════════")

eri_AAAA = ao_eri((A, A, A, A))
eri_BBBB = ao_eri((B, B, B, B))
eri_AABB = ao_eri((A, A, B, B))
eri_ABAB = ao_eri((A, B, A, B))
eri_ABBA = ao_eri((A, B, B, A))

print(f"(AA|AA) = {eri_AAAA:.16f}")
print(f"(BB|BB) = {eri_BBBB:.16f}")
print(f"(AA|BB) = {eri_AABB:.16f}")
print(f"(AB|AB) = {eri_ABAB:.16f}")
print(f"(AB|BA) = {eri_ABBA:.16f}")

# Verify: (AA|AA) should equal (BB|BB) by symmetry (identical atoms)
print(f"\n(AA|AA) - (BB|BB) = {eri_AAAA - eri_BBBB:.2e}  (should be 0)")
print(f"(AB|AB) - (AB|BA) = {eri_ABAB - eri_ABBA:.2e}  (should be 0 for real orbs)")

# ══════════════════════════════════════════════════════════
#  Overlap integral for MO construction
# ══════════════════════════════════════════════════════════

S_AB = 0.0
for i in range(n_prim):
    for j in range(n_prim):
        S_AB += coeff[i] * coeff[j] * overlap_1d(alpha[i], alpha[j], A, B)

print(f"\nOverlap S_AB = {S_AB:.16f}")

# MO coefficients
# σ_g = (s_A + s_B) / sqrt(2 + 2S)
# σ_u = (s_A - s_B) / sqrt(2 - 2S)
norm_g = 1.0 / sqrt(2.0 + 2.0 * S_AB)
norm_u = 1.0 / sqrt(2.0 - 2.0 * S_AB)

print(f"norm_g = 1/√(2+2S) = {norm_g:.16f}")
print(f"norm_u = 1/√(2-2S) = {norm_u:.16f}")

# ══════════════════════════════════════════════════════════
#  Transform to MO basis
# ══════════════════════════════════════════════════════════

# MO coefficient matrix: C[ao, mo]
# ao: 0=s_A, 1=s_B
# mo: 0=σ_g, 1=σ_u
C = np.array([[norm_g, norm_u],
              [norm_g, -norm_u]])

# MO ERI: (pq|rs) = Σ C[μp] C[νq] C[λr] C[σs] (μν|λσ)
# where μ,ν,λ,σ run over AOs (A, B)

# Build the full AO ERI tensor
centers_list = [A, B]
ao_eri_tensor = np.zeros((2, 2, 2, 2))
for mu in range(2):
    for nu in range(2):
        for lam in range(2):
            for sig in range(2):
                ao_eri_tensor[mu, nu, lam, sig] = ao_eri(
                    (centers_list[mu], centers_list[nu], centers_list[lam], centers_list[sig]))

print("\nAO ERI tensor:")
for mu in range(2):
    for nu in range(2):
        for lam in range(2):
            for sig in range(2):
                v = ao_eri_tensor[mu, nu, lam, sig]
                if abs(v) > 1e-15:
                    labels = ['A', 'B']
                    print(f"  ({labels[mu]}{labels[nu]}|{labels[lam]}{labels[sig]}) = {v:.16f}")

# Transform: (pq|rs)_MO = Σ C[μ,p] C[ν,q] C[λ,r] C[σ,s] * (μν|λσ)_AO
mo_eri = np.einsum('mp,nq,lr,os,mnlo->pqrs', C, C, C, C, ao_eri_tensor)

print("\n═══════════════════════════════════════════════════════")
print(" MO two-electron integrals (chemist notation [pq|rs])")
print("═══════════════════════════════════════════════════════")
labels_mo = ['σ_g', 'σ_u']
for p in range(2):
    for q in range(2):
        for r in range(2):
            for s in range(2):
                v = mo_eri[p, q, r, s]
                if abs(v) > 1e-15:
                    print(f"  [{p}{q}|{r}{s}] = {v:.16f}  ({labels_mo[p]}{labels_mo[q]}|{labels_mo[r]}{labels_mo[s]})")

print("\n═══════════════════════════════════════════════════════")
print(" Key integrals comparison")
print("═══════════════════════════════════════════════════════")
print(f"  [00|00] = {mo_eri[0,0,0,0]:.16f}  (our: 0.6744887663049631)")
print(f"  [11|11] = {mo_eri[1,1,1,1]:.16f}  (our: 0.6973979494693556)")
print(f"  [00|11] = {mo_eri[0,0,1,1]:.16f}  (our: 0.6636340478615040)")
print(f"  [01|10] = {mo_eri[0,1,1,0]:.16f}  (our: 0.6975782468828187 ← WRONG)")
print(f"  [01|01] = {mo_eri[0,1,0,1]:.16f}  (should = [01|10] by symmetry)")
print(f"  [01|10] = K₀₁ = exchange integral")

# Also compute one-electron integrals
def kinetic_1d(a, b, Ra, Rb):
    """Kinetic energy integral between two s-type Gaussians."""
    p = a + b
    mu = a * b / p
    RAB2 = np.sum((Ra - Rb)**2)
    S = (pi / p)**1.5 * exp(-mu * RAB2)
    T = mu * (3.0 - 2.0 * mu * RAB2) * S
    return T

def nuclear_attraction(a, b, Ra, Rb, Rc, Z):
    """Nuclear attraction integral for s-type Gaussians with nucleus at Rc."""
    p = a + b
    Rp = (a * Ra + b * Rb) / p
    RAB2 = np.sum((Ra - Rb)**2)
    RPC2 = np.sum((Rp - Rc)**2)
    T = p * RPC2
    return -Z * 2.0 * pi / p * exp(-a * b / p * RAB2) * boys_F0(T)

# One-electron integrals in AO basis
h_ao = np.zeros((2, 2))
for i in range(2):
    for j in range(2):
        Ri = centers_list[i]
        Rj = centers_list[j]
        # Kinetic
        T_ij = 0.0
        for a_idx in range(n_prim):
            for b_idx in range(n_prim):
                T_ij += coeff[a_idx] * coeff[b_idx] * kinetic_1d(alpha[a_idx], alpha[b_idx], Ri, Rj)
        # Nuclear attraction (Z=1 for each H atom)
        V_ij = 0.0
        for a_idx in range(n_prim):
            for b_idx in range(n_prim):
                V_ij += coeff[a_idx] * coeff[b_idx] * (
                    nuclear_attraction(alpha[a_idx], alpha[b_idx], Ri, Rj, A, 1.0) +
                    nuclear_attraction(alpha[a_idx], alpha[b_idx], Ri, Rj, B, 1.0)
                )
        h_ao[i, j] = T_ij + V_ij

# Transform to MO basis
h_mo = C.T @ h_ao @ C

print(f"\nOne-electron integrals:")
print(f"  h[0,0] = {h_mo[0,0]:.16f}  (our: -1.2563390730032498)")
print(f"  h[1,1] = {h_mo[1,1]:.16f}  (our: -0.4718960244306283)")
print(f"  h[0,1] = {h_mo[0,1]:.16e}  (should be ~0)")

# Nuclear repulsion
V_nn = 1.0 / R_bohr
print(f"\nNuclear repulsion = {V_nn:.16f}  (our: 0.7151043390810812)")

# Energies
E_HF_elec = 2.0 * h_mo[0,0] + mo_eri[0,0,0,0]
E_HF_total = E_HF_elec + V_nn
print(f"\nE_HF (electronic) = {E_HF_elec:.16f}")
print(f"E_HF (total) = {E_HF_total:.16f}")

# FCI 2x2
diag_00 = E_HF_elec
diag_11 = 2.0 * h_mo[1,1] + mo_eri[1,1,1,1]
off_diag = mo_eri[0,1,0,1]  # This is K₀₁ in chemist notation
avg = (diag_00 + diag_11) / 2.0
diff = (diag_00 - diag_11) / 2.0
E_FCI_elec = avg - sqrt(diff**2 + off_diag**2)
E_FCI_total = E_FCI_elec + V_nn

print(f"\nFCI ground state:")
print(f"  Diagonal [1100] = {diag_00:.16f}")
print(f"  Diagonal [0011] = {diag_11:.16f}")
print(f"  Off-diagonal K₀₁ = {off_diag:.16f}")
print(f"  E_FCI (electronic) = {E_FCI_elec:.16f}")
print(f"  E_FCI (total) = {E_FCI_total:.16f}")
print(f"  Literature E_FCI(H₂/STO-3G) ≈ -1.1373 Ha")
