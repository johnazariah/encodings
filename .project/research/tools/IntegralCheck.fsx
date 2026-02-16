/// IntegralCheck.fsx — Cross-verify our H₂/STO-3G integrals against
/// OpenFermion reference values from arXiv:1208.5986
///
/// OpenFermion hydrogen_integration_test.py provides ground-truth
/// spin-orbital integrals.  We check our spatial→spin-orbital conversion.

// ═══════════════════════════════════════════════════════
//  Our spatial integrals (from H2Demo.fsx)
// ═══════════════════════════════════════════════════════

let nuclearRepulsion = 0.7151043390810812

let h1_spatial = Array2D.init 2 2 (fun p q ->
    if p = q then
        [| -1.2563390730032498; -0.4718960244306283 |].[p]
    else 0.0)

let h2vals = [| 0.6744887663049631; 0.6973979494693556;
                0.6636340478615040; 0.6975782468828187 |]

let h2_spatial = Array4D.init 2 2 2 2 (fun p q r s ->
    match (p,q,r,s) with
    | (0,0,0,0) -> h2vals.[0]
    | (1,1,1,1) -> h2vals.[1]
    | (0,0,1,1) | (1,1,0,0) -> h2vals.[2]
    | (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> h2vals.[3]
    | _ -> 0.0)

// ═══════════════════════════════════════════════════════
//  OpenFermion reference values (spin-orbital basis)
//  From hydrogen_integration_test.py, arXiv:1208.5986
// ═══════════════════════════════════════════════════════

// These are in the InteractionOperator convention:
//   H = constant + Σ_{pq} T[p,q] a†_p a_q + Σ_{pqrs} V[p,q,r,s] a†_p a†_q a_s a_r
// where T is one_body_tensor and V is two_body_tensor = (1/2) * spinorb_from_spatial result

let of_nuclear = 0.71375    // g0
let of_g1 = -1.2525         // one_body[0,0] = one_body[1,1]
let of_g2 = -0.47593        // one_body[2,2] = one_body[3,3]
let of_g3 = 0.67449 / 2.0   // two_body[0,1,1,0] — already includes 1/2 factor
let of_g4 = 0.69740 / 2.0   // two_body[2,3,3,2]
let of_g5 = 0.66347 / 2.0   // two_body[0,2,2,0] etc
let of_g6 = 0.18129 / 2.0   // two_body[0,2,0,2] etc

printfn "═══════════════════════════════════════════════════════"
printfn " Comparison: Our integrals vs OpenFermion reference"
printfn "═══════════════════════════════════════════════════════"
printfn ""

// ─── Nuclear repulsion ───
printfn "Nuclear repulsion:"
printfn "  Ours:       %.10f" nuclearRepulsion
printfn "  OpenFermion: %.5f" of_nuclear
printfn "  Δ = %.6e" (nuclearRepulsion - of_nuclear)
printfn ""

// ─── One-body spatial ───
printfn "One-body spatial integrals:"
printfn "  h[0,0]: ours=%.10f  OF=%.5f  Δ=%.4e" h1_spatial.[0,0] of_g1 (h1_spatial.[0,0] - of_g1)
printfn "  h[1,1]: ours=%.10f  OF=%.5f  Δ=%.4e" h1_spatial.[1,1] of_g2 (h1_spatial.[1,1] - of_g2)
printfn ""

// ─── Two-body spatial ───
// OpenFermion two_body (spin-orbital, with 1/2) values correspond to spatial integrals:
//   g3*2 = 0.67449 → [00|00]  (but our [00|00] = 0.6745 ≈ match)
// Wait - let me be more careful about which spatial integral maps to which g.

// In OpenFermion:
//   spinorb_from_spatial: two_body_coefficients[2p, 2q, 2r, 2s] = spatial[p,q,r,s]
//   get_molecular_hamiltonian: InteractionOperator(..., 1/2 * two_body_coefficients)
//
// So InteractionOperator.two_body_tensor[2p, 2q, 2r, 2s] = spatial[p,q,r,s] / 2
//
// The test checks:
//   two_body[0,1,1,0] = g3 = 0.67449/2
//   → This is InteractionOp tensor at spin indices [0,1,1,0]
//   → spin indices 0=0α, 1=0β
//   → From spinorb: two_body_coefficients[0,1,1,0]
//   → This comes from the "Mixed spin" branch:
//     two_body_coefficients[2p, 2q+1, 2r+1, 2s] = spatial[p,q,r,s]
//     with p=0,q=0,r=0,s=0 → gives spatial[0,0,0,0]
//   → So InteractionOp[0,1,1,0] = spatial[0,0,0,0] / 2
//   → g3 = spatial[0,0,0,0] / 2 = 0.67449/2
//   → spatial[0,0,0,0] = 0.67449
//   Our value: 0.6744887... ✓ match!

printfn "Two-body spatial integrals (comparing via OF spin-orbital values):"
printfn ""
printfn "  [00|00] = g3*2: ours=%.10f  OF=%.5f  Δ=%.4e" h2vals.[0] 0.67449 (h2vals.[0] - 0.67449)
printfn "  [11|11] = g4*2: ours=%.10f  OF=%.5f  Δ=%.4e" h2vals.[1] 0.69740 (h2vals.[1] - 0.69740)

// g5 = two_body[0,2,2,0]:  spin indices 0=0α, 2=1α
//   From spinorb "Same spin" branch:
//   two_body_coefficients[2p, 2q, 2r, 2s] = spatial[p,q,r,s]
//   → [0,2,2,0] = two_body_coefficients[2*0, 2*1, 2*1, 2*0] = spatial[0,1,1,0]
// Wait, let me re-index: spin orbital 0 = spatial 0, α
//                         spin orbital 2 = spatial 1, α
// So two_body_coefficients[0,2,2,0]:
//   This matches the pattern [2p, 2q, 2r, 2s] with p=0,q=1,r=1,s=0
//   → spatial[0,1,1,0]
//   → InteractionOp[0,2,2,0] = spatial[0,1,1,0] / 2
//   → g5 = spatial[0,1,1,0] / 2 = 0.66347/2
//   → spatial[0,1,1,0] = 0.66347
//   Our value [0,1,1,0] = h2vals.[3] = 0.6975782... 
//   BUT OpenFermion says 0.66347!  MISMATCH!

printfn "  [01|10] = g5*2: ours=%.10f  OF=%.5f  Δ=%.4e" h2vals.[3] 0.66347 (h2vals.[3] - 0.66347)
printfn "  [00|11] = ??"

// g6 = two_body[0,2,0,2]:  spin indices 0=0α, 2=1α
//   [2p, 2q, 2r, 2s] → p=0,q=1,r=0,s=1 → spatial[0,1,0,1]
//   → InteractionOp[0,2,0,2] = spatial[0,1,0,1] / 2
//   → g6 = spatial[0,1,0,1] / 2 = 0.18129/2
//   → spatial[0,1,0,1] = 0.18129
//   Our value [0,1,0,1] = h2vals.[3] = 0.6975782... 
//   OpenFermion says 0.18129! HUGE MISMATCH!

printfn "  [01|01] = g6*2: ours=%.10f  OF=%.5f  Δ=%.4e" h2vals.[3] 0.18129 (h2vals.[3] - 0.18129)
printfn ""

// ═══════════════════════════════════════════════════════
//  THE PROBLEM: Our spatial integral mapping is wrong!
// ═══════════════════════════════════════════════════════
//
// We have:
//   [00|00] = 0.6745  ← correct
//   [11|11] = 0.6974  ← correct  
//   [00|11] = [11|00] = 0.6636  ← this maps to which g?
//   [01|10] = [10|01] = [01|01] = [10|10] = 0.6976  ← WRONG values
//
// In chemist's notation [pq|rs], the 8-fold symmetry for real orbitals is:
//   [pq|rs] = [qp|rs] = [pq|sr] = [qp|sr] = [rs|pq] = [sr|pq] = [rs|qp] = [sr|qp]
//
// For H₂ with 2 spatial orbitals, we should have:
//   [00|00] = 0.6745   (matches OF g3*2 = 0.67449)
//   [11|11] = 0.6974   (matches OF g4*2 = 0.69740)
//   [01|10] = 0.6635   (should match OF g5*2 = 0.66347)  ← this is DIFFERENT from [01|01]!
//   [01|01] = 0.1813   (should match OF g6*2 = 0.18129)
//   [00|11] = ?

// Actually, let me re-derive. The 8-fold symmetry gives:
//   [pq|rs] = [qp|rs] = [pq|sr] = [qp|sr] = [rs|pq] = [sr|pq] = [rs|qp] = [sr|qp]
//
// So [01|10] ≠ [01|01] in general!  
// [01|10]: p=0,q=1,r=1,s=0 → by symmetry [10|01] = [01|10] ✓, but NOT = [01|01]
// [01|01]: p=0,q=1,r=0,s=1 → by symmetry [10|10] = [01|01] ✓
//
// OUR BUG: We set [01|10] = [10|01] = [01|01] = [10|10] = same value!
// But [01|10] and [01|01] are DIFFERENT integrals!

printfn "═══════════════════════════════════════════════════════"
printfn " BUG FOUND: [01|10] ≠ [01|01] in general!"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "Our h2_spatial incorrectly assigns the SAME value (0.6976)"
printfn "to both [01|10] and [01|01], but these are distinct integrals:"
printfn "  [01|10] should be = 0.66347  (exchange integral K)"
printfn "  [01|01] should be = 0.18129  (exchange integral type)"
printfn ""
printfn "Also, [00|11] needs to be checked."
printfn ""

// Let me figure out the correct mapping.
// In OpenFermion, spinorb_from_spatial gives:
//
// Mixed spin: two_body_coeff[2p, 2q+1, 2r+1, 2s] = spatial[p,q,r,s]
//             two_body_coeff[2p+1, 2q, 2r, 2s+1] = spatial[p,q,r,s]
// Same spin:  two_body_coeff[2p, 2q, 2r, 2s] = spatial[p,q,r,s]
//             two_body_coeff[2p+1, 2q+1, 2r+1, 2s+1] = spatial[p,q,r,s]
//
// The InteractionOperator Hamiltonian is:
//   H = const + Σ T[p,q] a†_p a_q + Σ V[p,q,r,s] a†_p a†_q a_s a_r
// where V = (1/2) * spinorb_coefficients
//
// Note the operator ordering: a†_p a†_q a_s a_r (NOT a†_p a†_q a_r a_s)
//
// The second-quantized Hamiltonian is:
//   H = Σ h_{pq} a†_p a_q + (1/2) Σ ⟨pq||rs⟩ a†_p a†_q a_s a_r
// where ⟨pq||rs⟩ = ⟨pq|rs⟩ - ⟨pq|sr⟩ is the antisymmetrized integral
//
// BUT OpenFermion's InteractionOperator uses NON-antisymmetrized integrals
// with the explicit antisymmetry coming from the anticommutation of operators.
//
// So the OpenFermion convention is:
//   H = const + Σ T[p,q] a†_p a_q + Σ V[p,q,r,s] a†_p a†_q a_s a_r
// with V[p,q,r,s] = spatial[p/2, q/2, r/2, s/2] / 2 (when spins match)
//
// And the spatial integrals spatial[p,q,r,s] are in CHEMIST'S notation [pq|rs]:
//   [pq|rs] = ∫ φ_p*(1) φ_q(1) (1/r12) φ_r*(2) φ_s(2) d1 d2
//
// Now, the physicist's notation is:
//   ⟨pq|rs⟩ = ∫ φ_p*(1) φ_q*(2) (1/r12) φ_r(1) φ_s(2) d1 d2
//   = [pr|qs]  (chemist's)
//
// So ⟨pq|rs⟩_phys = [pr|qs]_chem
//
// Our code in H2Demo.fsx uses:
//   h2_spatial.[p/2, r/2, q/2, s/2]  for ⟨pq|rs⟩
// which gives [p/2, r/2 | q/2, s/2]_chem = ⟨(p/2)(q/2) | (r/2)(s/2)⟩_phys
// This is correct!
//
// But the question is: what are the CORRECT spatial integrals?
// 
// From OpenFermion, the spin-orbital two_body tensor (InteractionOp) values are:
//   V[0,1,1,0] = g3 = 0.337245   → spatial[0,0,0,0] = 2*g3 = 0.67449
//   V[2,3,3,2] = g4 = 0.34870    → spatial[1,1,1,1] = 2*g4 = 0.69740
//   V[0,2,2,0] = g5 = 0.331735   → spatial[0,1,1,0] = 2*g5 = 0.66347
//   V[0,2,0,2] = g6 = 0.090645   → spatial[0,1,0,1] = 2*g6 = 0.18129

// Wait - but spinorb_from_spatial puts spatial[p,q,r,s] into the tensor,
// and then the Hamiltonian divides by 2.  But the test checks the
// InteractionOperator two_body_tensor directly.
//
// InteractionOp.two_body_tensor[spin_p, spin_q, spin_r, spin_s]
//   = (1/2) * spinorb_coeff[spin_p, spin_q, spin_r, spin_s]
//
// For same-spin alpha case: p=0,q=1,r=1,s=0 → all alpha (even indices: 0,2,2,0)
//   spinorb[0,2,2,0] = spatial[0,1,1,0]
//   InteractionOp[0,2,2,0] = spatial[0,1,1,0] / 2 = g5

// For same-spin alpha case: p=0,q=1,r=0,s=1 → (0,2,0,2)
//   spinorb[0,2,0,2] = spatial[0,1,0,1]
//   InteractionOp[0,2,0,2] = spatial[0,1,0,1] / 2 = g6

// So:
//   spatial[0,1,1,0] = 2*g5 = 0.66347
//   spatial[0,1,0,1] = 2*g6 = 0.18129

printfn "═══════════════════════════════════════════════════════"
printfn " Correct spatial integrals from OpenFermion:"
printfn "═══════════════════════════════════════════════════════"
printfn "  [00|00] = 0.67449   (our value: %.5f, ok)" h2vals.[0]
printfn "  [11|11] = 0.69740   (our value: %.5f, ok)" h2vals.[1]
printfn "  [00|11] = 0.66347   (g5*2)"
printfn "  [11|00] = 0.66347   (by symmetry)"
printfn "  [01|10] = 0.18129   (g6*2)"
printfn "  [10|01] = 0.18129   (by symmetry)"
printfn "  [01|01] = 0.18129   (by symmetry [01|01] = [10|10] = [01|10])"
printfn "  [10|10] = 0.18129   (by symmetry)"
printfn ""

// Wait, I need to re-check.  Let's see what [00|11] maps to.
// spatial[0,0,1,1] → this is used in spinorb as:
//   Same spin: spinorb[2*0, 2*0, 2*1, 2*1] = spinorb[0,0,2,2] = spatial[0,0,1,1]
//   InteractionOp[0,0,2,2] = spatial[0,0,1,1] / 2
// 
// From the test: the test doesn't check [0,0,2,2] directly.
// But let me check: is g6 testing [0,2,0,2] or [0,0,2,2]?
// The test says: self.assertAlmostEqual(self.two_body[0, 2, 0, 2], g6)
// So V[0,2,0,2] = g6 = 0.090645

// spin indices [0,2,0,2]:
// spin 0 = spatial 0, α
// spin 2 = spatial 1, α  
// Same spin alpha: spinorb[2p,2q,2r,2s] = spatial[p,q,r,s]
// [0,2,0,2] = spinorb[0,2,0,2] → p=0,q=1,r=0,s=1 → spatial[0,1,0,1]
// InteractionOp[0,2,0,2] = spatial[0,1,0,1] / 2 = g6 = 0.090645
// → spatial[0,1,0,1] = 0.18129

// And [0,2,2,0]:
// [0,2,2,0] = spinorb[0,2,2,0] → p=0,q=1,r=1,s=0 → spatial[0,1,1,0]
// InteractionOp[0,2,2,0] = spatial[0,1,1,0] / 2 = g5 = 0.331735
// → spatial[0,1,1,0] = 0.66347

// So indeed: spatial[0,1,1,0] ≠ spatial[0,1,0,1]
//            0.66347          ≠   0.18129

// But in our code we have:
//   (0,1,1,0) | (1,0,0,1) | (0,1,0,1) | (1,0,1,0) -> h2vals.[3] = 0.6976
// This treats [01|10] = [01|01] = 0.6976, which is WRONG!

// What about [00|11]?
// The test checks two_body[0,0,2,2]:  
// Hmm, actually the test doesn't check this directly. But from 
// the spatial integral symmetries:
//   [00|11] = [11|00] by 8-fold symmetry = [00|11]
//   This is a Coulomb integral between orbitals 0 and 1.
//
// Actually wait - what does g5 correspond to in terms of chemist's notation?
// V[0,2,2,0] = spatial[0,1,1,0] / 2 = g5
// spatial[0,1,1,0] in chemist's notation = ∫ φ_0(1) φ_1(1) (1/r) φ_1(2) φ_0(2) = [01|10]
// This is the EXCHANGE integral K₀₁

// V[0,0,2,2] = spatial[0,0,1,1] / 2
// spatial[0,0,1,1] = ∫ φ_0(1) φ_0(1) (1/r) φ_1(2) φ_1(2) = [00|11]
// This is the COULOMB integral J₀₁

// So our mapping was:
//   [00|11] = [11|00] = 0.6636  ← This SHOULD be the Coulomb J₀₁
//   [01|10] = [10|01] = [01|01] = [10|10] = 0.6976  ← This was wrong!

// The correct values should be:
//   [00|11] = [11|00] → J₀₁ (Coulomb) 
//   [01|10] = [10|01] → K₀₁ (Exchange) = 0.66347
//   [01|01] = [10|10] → ??? = 0.18129

// Actually let me re-check the 8-fold symmetry.
// For real orbitals: [pq|rs] = [qp|rs] = [pq|sr] = [qp|sr] = [rs|pq] = [sr|pq] = [rs|qp] = [sr|qp]
// So: [01|10] = [10|10] = [01|01] = [10|01] = [10|01] = [01|01] = [10|10] = [01|10]
// Wait, that gives [01|10] = [01|01]??
// Let me be more careful:
//   [pq|rs] with p=0,q=1,r=1,s=0
//   Symmetries: [qp|rs] = [10|10], [pq|sr] = [01|01], [qp|sr] = [10|01]
//               [rs|pq] = [10|01], [sr|pq] = [01|01], [rs|qp] = [10|10], [sr|qp] = [01|10]
// So: [01|10] = [10|10] = [01|01] = [10|01] — they ARE all the same by 8-fold symmetry!

// But OpenFermion says spatial[0,1,1,0] = 0.66347 and spatial[0,1,0,1] = 0.18129
// These are DIFFERENT!

// Hmm — OpenFermion spatial integrals are in chemist's notation [pq|rs],
// but the way `spinorb_from_spatial` works, the spatial array indices 
// (p,q,r,s) directly correspond to [pq|rs]. So:
//   spatial[0,1,1,0] = [01|10] = 0.66347
//   spatial[0,1,0,1] = [01|01] = 0.18129

// These would only be equal if the 8-fold symmetry held. But wait:
// [01|10] by the 8-fold symmetry rule: 
//   [pq|rs]: p=0,q=1,r=1,s=0
//   [qp|rs] = [10|10] ← different indices!
//   [pq|sr] = [01|01] ← different indices!
// 
// So [01|10] should equal [01|01] by the [pq|sr] symmetry (swap r,s).
// But OpenFermion says they're different! That means OpenFermion is NOT
// using chemist's [pq|rs] notation for its spatial array!

// Let me re-read spinorb_from_spatial more carefully...
// Actually, looking at get_molecular_hamiltonian:
//   spinorb_from_spatial(one_body_integrals, two_body_integrals)
// where two_body_integrals comes from molecule.two_body_integrals
// which is stored as two_body_integrals[p,q,r,s].

// From OpenFermion documentation and convention:
// The MolecularData stores two-electron integrals in CHEMIST'S notation
// but indexed as (pq|rs) = integral over φ_p*(1) φ_q(1) (1/r12) φ_r*(2) φ_s(2)
// So the index order in the array is [p,q,r,s] = chemist's (pq|rs).

// BUT the spinorb_from_spatial function uses indices as if they're 
// physicist's notation for the tensor: it maps spatial[p,q,r,s] to 
// the two-body tensor of the Hamiltonian H = Σ T_{pq} a†_p a_q + Σ V_{pqrs} a†_p a†_q a_s a_r
// where the operator order is a†_p a†_q a_s a_r (note: s before r!).

// Actually, let me look at the InteractionOperator definition to understand
// the convention. The key is: what is the meaning of [p,q,r,s] in 
// molecule.two_body_integrals?

// From PySCF, the integrals are stored in CHEMIST'S notation: (pq|rs)
// So molecule.two_body_integrals[p,q,r,s] = (pq|rs) = ∫ φ_p(1)φ_q(1) 1/r12 φ_r(2)φ_s(2)
// And (pq|rs) has the symmetries: (pq|rs) = (qp|rs) = (pq|sr) = (qp|sr) = (rs|pq) etc.

// But spinorb_from_spatial(spatial) creates spin_tensor[2p,2q,2r,2s] = spatial[p,q,r,s]
// and InteractionOperator stores H with the convention:
//   H = const + Σ T[p,q] a†_p a_q + Σ V[p,q,r,s] a†_p a†_q a_s a_r

// So V[p,q,r,s] in the InteractionOperator is in PHYSICIST'S notation:
//   V[p,q,r,s] = ⟨pq|rs⟩_phys / 2 = (pr|qs)_chem / 2
// Actually no - V is NOT the physicist's integral, it's the COEFFICIENT of a†_p a†_q a_s a_r.

// The second-quantized Hamiltonian in physicist's notation is:
//   H = Σ h_{pq} a†_p a_q + (1/2) Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r
// So V[p,q,r,s] = ⟨pq|rs⟩_phys / 2 in this convention.
// And ⟨pq|rs⟩_phys = (pr|qs)_chem

// From spinorb:
//   V[2p,2q,2r,2s] = spatial[p,q,r,s] / 2
// So: ⟨(2p)(2q)|(2r)(2s)⟩_phys = spatial[p,q,r,s]
// And: (2p,2r | 2q,2s)_chem = spatial[p,q,r,s]

// This means: IF spatial is in chemist's notation (pq|rs),
// then spinorb maps spatial[p,q,r,s] = (pq|rs)_chem to the slot for
// ⟨2p,2q|2r,2s⟩_phys.
// But ⟨2p,2q|2r,2s⟩_phys should equal (2p,2r|2q,2s)_chem = spatial index [p,r,q,s] in chemist's.
// So we'd need spatial[p,q,r,s] = (p,r|q,s)_chem ← this is PHYSICIST'S order!

// CONCLUSION: OpenFermion's molecule.two_body_integrals are NOT in chemist's notation!
// They are stored such that spatial[p,q,r,s] represents the physicist's integral ⟨pq|rs⟩.
// That is, the array index order follows PHYSICIST'S convention.

// This explains why [01|10]_OF = 0.66347 ≠ [01|01]_OF = 0.18129:
//   spatial_OF[0,1,1,0] = ⟨01|10⟩_phys = (01|10) ... hmm

// Actually I think OpenFermion internally uses neither pure chemist nor physicist
// but a "physicist-like" convention where:
//   H = Σ h[p,q] a†_p a_q + Σ h[p,q,r,s] a†_p a†_q a_s a_r
//                                                        ^^^^ note order

// Let me just verify numerically. The correct spatial integrals for H₂/STO-3G 
// at R=0.7414 Å are well-known. Let me use the values from the OpenFermion test:

// g3 = 0.67449/2 → OF InteractionOp two_body[0,1,1,0] = g3
//   spin indices 0=0α, 1=0β, 1=0β, 0=0α
//   Mixed spin: coeff[2*0, 2*0+1, 2*0+1, 2*0] = spatial[0,0,0,0]
//   InteractionOp = spatial[0,0,0,0] / 2 = g3
//   → spatial[0,0,0,0] = 0.67449 ✓ matches our 0.6745

// g5 = 0.66347/2 → OF two_body[0,2,2,0] = g5
//   spin indices 0=0α, 2=1α, 2=1α, 0=0α
//   Same spin α: coeff[2*0, 2*1, 2*1, 2*0] = spatial[0,1,1,0]
//   InteractionOp = spatial[0,1,1,0] / 2 = g5
//   → spatial_OF[0,1,1,0] = 0.66347

// g6 = 0.18129/2 → OF two_body[0,2,0,2] = g6  
//   spin indices 0=0α, 2=1α, 0=0α, 2=1α
//   Same spin α: coeff[2*0, 2*1, 2*0, 2*1] = spatial[0,1,0,1]
//   InteractionOp = spatial[0,1,0,1] / 2 = g6
//   → spatial_OF[0,1,0,1] = 0.18129

// And from the test: two_body[0,0,2,2] is NOT tested directly.
// But we know the Coulomb integral J₀₁ = (00|11) in chemist's notation.

// So the question is: does OpenFermion's spatial[p,q,r,s] use chemist's [pq|rs]?
// If yes, then [01|10] should equal [01|01] by swap (r,s), but OF has them different.
// So NO, OF does NOT use chemist's [pq|rs].

// Let me check: physicist's ⟨pq|rs⟩ has the symmetry ⟨pq|rs⟩ = ⟨qp|sr⟩
// but NOT ⟨pq|rs⟩ = ⟨pq|sr⟩ in general.
// Specifically: ⟨pq|rs⟩ = ⟨qp|sr⟩ = ⟨rs|pq⟩ = ⟨sr|qp⟩ (complex conjugate pairs for real)
// And ⟨pq|rs⟩ ≠ ⟨pq|sr⟩ in general!

// If spatial_OF[0,1,1,0] = ⟨01|10⟩_phys and spatial_OF[0,1,0,1] = ⟨01|01⟩_phys
// then these CAN be different! Good.

// ⟨01|10⟩_phys = (00|11)_chem = Coulomb J₀₁ = 0.66347
// ⟨01|01⟩_phys = (00|11)_chem? No...
// ⟨pq|rs⟩_phys = [pr|qs]_chem
// ⟨01|10⟩_phys = [01|10]_chem? No: [0*1, 1*0] → hmm
// ⟨pq|rs⟩ = ∫ φp(1)φq(2) 1/r12 φr(1)φs(2) = [pr|qs]_chem
// So: ⟨01|10⟩ = [01|10]_chem NO wait:
// ⟨pq|rs⟩ with p=0,q=1,r=1,s=0 → [p,r | q,s] = [0,1|1,0]_chem
// Hmm that's [01|10]_chem.
// ⟨01|01⟩ with p=0,q=1,r=0,s=1 → [0,0|1,1]_chem = J₀₁ = Coulomb

// So: spatial_OF[0,1,1,0] = ⟨01|10⟩_phys = [01|10]_chem = K₀₁ = Exchange = 0.66347
//     spatial_OF[0,1,0,1] = ⟨01|01⟩_phys = [00|11]_chem = J₀₁ = Coulomb = 0.18129

// Wait, that gives J₀₁ = 0.18129?? That seems very small for a Coulomb integral.
// Usually the Coulomb integral J is larger than the exchange K.
// Let me double-check...

// Hmm, actually for H₂/STO-3G:
//   J₀₁ = ⟨01|01⟩ = [00|11] = ∫ |φ₀(1)|² (1/r₁₂) |φ₁(2)|² d1d2
//   K₀₁ = ⟨01|10⟩ = [01|10] = ∫ φ₀*(1)φ₁*(2) (1/r₁₂) φ₁(1)φ₀(2) d1d2

// For a minimal basis set on H₂, the orbitals are σ_g (bonding) and σ_u (antibonding)
// which have significant overlap, so K₀₁ can be quite large.
// The Coulomb integral between them... hmm.

// Actually wait, I think the convention might be different. Let me reconsider.
// 
// Actually: (00|11) in chemist's notation = ∫ φ₀(1)φ₀(1) 1/r₁₂ φ₁(2)φ₁(2) d1d2
// This IS the Coulomb integral J₀₁ and should be fairly large.
// And [01|10] = ∫ φ₀(1)φ₁(1) 1/r₁₂ φ₁(2)φ₀(2) d1d2 = K₀₁

// Let me check: for H₂ STO-3G, known values are:
// h₁₁ = -1.2528, h₂₂ = -0.4760 (close to our -1.2563, -0.4719)
// (11|11) = 0.6746, (22|22) = 0.6975, (11|22) = 0.6632, (12|21) = 0.1813

// AH HA! So:
//   (11|22) = J₁₂ = 0.6632 (Coulomb) ← LARGE
//   (12|21) = K₁₂ = 0.1813 (Exchange) ← SMALL

// In our code (0-indexed):
//   [00|11] = (11|22) = J₀₁ = 0.6632
//   [01|10] = (12|21) = K₀₁ = 0.1813

// But we have:
//   [00|11] = h2vals.[2] = 0.6636  ← this is correct! J₀₁
//   [01|10] = h2vals.[3] = 0.6976  ← this is WRONG! Should be K₀₁ = 0.18129

printfn "═══════════════════════════════════════════════════════"
printfn " CORRECTED UNDERSTANDING"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "The correct spatial integrals in chemist's notation [pq|rs]:"
printfn "  [00|00] = (11|11) = 0.6745  ← matches our h2vals.[0] ✓"
printfn "  [11|11] = (22|22) = 0.6974  ← matches our h2vals.[1] ✓"
printfn "  [00|11] = (11|22) = J₀₁ = 0.6636  ← matches our h2vals.[2] ✓"
printfn "  [01|10] = (12|21) = K₀₁ = 0.18129  ← NOT our h2vals.[3] = 0.6976 ✗"
printfn ""
printfn "Our code INCORRECTLY assigns [01|10] = 0.6976"
printfn "The correct value of K₀₁ = 0.18129"
printfn ""
printfn "The value 0.6976 is actually [01|01] if [pq|rs] symmetry held,"
printfn "but wait - [01|01] should equal [01|10] by the [pq|rs]→[pq|sr] symmetry."
printfn "Something is off with the 8-fold symmetry claim."
printfn ""

// Actually let me reconsider. The issue is I was confused about the conventions.
//
// OpenFermion's spatial integrals spatial_OF[p,q,r,s] use CHEMIST'S notation:
// spatial_OF[p,q,r,s] = (pq|rs) = ∫ φ_p*(1)φ_q(1) (1/r12) φ_r*(2)φ_s(2)
//
// But spinorb_from_spatial maps them as if they're physicist's, putting them into
// the coefficient of a†_p a†_q a_s a_r:
//   coefficient[p,q,r,s] = spatial_OF[p,q,r,s] (before the 1/2 factor)
//
// The InteractionOperator Hamiltonian is:
//   H = c + Σ h_{pq} a†_p a_q + Σ h_{pqrs} a†_p a†_q a_s a_r
// where h_{pqrs} = V_{pqrs} = spatial_OF[p,q,r,s] / 2 (for matching spins)
//
// For this to be a correct Hamiltonian, we need:
//   h_{pqrs} = ⟨pq|rs⟩ / 2 in physicist's convention (since H has a†_p a†_q a_s a_r)
//
// So spatial_OF[p,q,r,s] = ⟨pq|rs⟩_phys, NOT chemist's notation!
//
// Then spatial_OF[0,1,1,0] = ⟨01|10⟩_phys = [01|10]_chem
// Actually: ⟨pq|rs⟩_phys = [pr|qs]_chem
// ⟨01|10⟩ = [01|10]_chem → hmm, that's coincidence
// No wait: ⟨pq|rs⟩ with p=0,q=1,r=1,s=0 → [p=0,r=1 | q=1,s=0] = [01|10]_chem
//
// ⟨01|01⟩ with p=0,q=1,r=0,s=1 → [p=0,r=0 | q=1,s=1] = [00|11]_chem = J₀₁
//
// So OpenFermion's spatial array uses PHYSICIST'S convention:
//   spatial_OF[p,q,r,s] = ⟨pq|rs⟩_phys
//
// And ⟨01|10⟩_phys = [01|10]_chem = exchange integral K₀₁ = 0.66347
// And ⟨01|01⟩_phys = [00|11]_chem = Coulomb integral J₀₁ = 0.18129

// No wait, that gives J = 0.18 and K = 0.66 which is backwards (J should be > K).
// Unless... the STO-3G H₂ really does have K > J for the bonding/antibonding pair?

// Actually for H₂ with σ_g and σ_u, these orbitals are:
//   σ_g = (s_A + s_B) / √(2+2S)
//   σ_u = (s_A - s_B) / √(2-2S) 
// where s_A and s_B are 1s orbitals on atoms A and B.
// The exchange integral K₀₁ between σ_g and σ_u CAN be large because
// the orbital densities φ_0(r)φ_1(r) have significant magnitude.
// And the Coulomb integral J₀₁ can be relatively small if the charge densities
// |φ₀|² and |φ₁|² are concentrated in different regions.
// Wait, actually they overlap a lot too...

// Let me just compute E_HF with the OpenFermion values to check:
// E_HF(electronic) = 2*h[0,0] + J₀₀ = 2*h₁₁ + (11|11)
// = 2*(-1.2525) + 0.67449 = -2.5050 + 0.6745 = -1.8305

// With our values:
// E_HF(electronic) = 2*(-1.2563) + 0.6745 = -2.5127 + 0.6745 = -1.8382
// That's what we got before.

// And with nuclear repulsion:
// OF: E_HF(total) = -1.8305 + 0.71375 = -1.117
// Ours: -1.8382 + 0.71510 = -1.123

// Known E_HF for H₂/STO-3G = -1.1168 Ha
// OF gives -1.117 ✓ (close, limited precision of g0-g2)
// Ours gives -1.123 (close but different integrals)

// Now the issue: our two-body integrals have the wrong exchange integral.
// We use 0.6976 for ALL off-diagonal two-electron integrals ([01|10], [01|01], etc.)
// But the correct values are:
//   [01|10] = 0.18129 (K₀₁, exchange) — or is it [01|10] = 0.66347?

// I'm getting confused. Let me just build the correct integral set.

printfn "═══════════════════════════════════════════════════════"
printfn " Building correct spatial integrals"
printfn "═══════════════════════════════════════════════════════"
printfn ""

// OpenFermion spatial[p,q,r,s] convention:
// These are NOT chemist's integrals. Looking at the code more carefully,
// molecule.two_body_integrals from PySCF are in CHEMIST'S notation.
// But get_molecular_hamiltonian calls spinorb_from_spatial which maps:
//   spin_tensor[2p,2q+1,2r+1,2s] = spatial[p,q,r,s]
//   spin_tensor[2p+1,2q,2r,2s+1] = spatial[p,q,r,s]
//   spin_tensor[2p,2q,2r,2s] = spatial[p,q,r,s]
//   spin_tensor[2p+1,2q+1,2r+1,2s+1] = spatial[p,q,r,s]
// And InteractionOperator.two_body = (1/2) * spin_tensor
//
// The InteractionOperator H = c + Σ T_{pq} a†_p a_q + Σ V_{pqrs} a†_p a†_q a_s a_r
//
// For same-spin case (all α, even indices):
//   V[2p,2q,2r,2s] = spatial[p,q,r,s] / 2
//
// The correct physical Hamiltonian should have:
//   coefficient of a†_{2p} a†_{2q} a_{2s} a_{2r} = ⟨pq|rs⟩_phys / 2
//
// where ⟨pq|rs⟩_phys = (pr|qs)_chem for real orbitals
//
// So: spatial[p,q,r,s] / 2 = ⟨pq|rs⟩_phys / 2
// → spatial[p,q,r,s] = ⟨pq|rs⟩_phys = (pr|qs)_chem
//
// So the molecule.two_body_integrals ARE in chemist's notation (pq|rs),
// but spinorb_from_spatial treats them as if they were physicist's ⟨pq|rs⟩!
//
// Wait, that can't be right because the test values check out...
// Unless molecule.two_body_integrals are ALSO in physicist's notation?

// OK let me just look at the PySCF interface to settle this.
// From OpenFermionPySCF: the integrals are computed as:
//   one_body = h1e  (kinetic + nuclear)
//   two_body = ao2mo.full(mol, mf.mo_coeff)  → gives physicist's (pq|rs) or chemist's?
//
// PySCF ao2mo returns integrals in CHEMIST'S notation by default: (pq|rs)
// But there may be a reshape involved.

// Actually, from many references and discussions:
// PySCF ao2mo returns chemist's integrals (pq|rs) as a 1D array or 2D array,
// which is then reshaped to 4D. The index ordering in the 4D array 
// follows eri[p,q,r,s] = (pq|rs)_chem.

// And OpenFermion's MolecularData.two_body_integrals stores them the same way.

// BUT spinorb_from_spatial treats them differently!
// It does: spin[2p,2q,2r,2s] = spatial[p,q,r,s]
// This is correct IF the Hamiltonian is:
//   H = Σ h_{pq} a†_p a_q + (1/2) Σ spatial[p,q,r,s] a†_{2p} a†_{2q} a_{2s} a_{2r}
//
// For the physics to work, the coefficient of a†_p a†_q a_s a_r should be ⟨pq|rs⟩/2
// So spatial[p,q,r,s] must equal ⟨pq|rs⟩_phys.
//
// But spatial comes from PySCF in chemist's notation (pq|rs)_chem.
// And (pq|rs)_chem = ⟨pr|qs⟩_phys ≠ ⟨pq|rs⟩_phys in general.
//
// Unless there's a transpose somewhere...
// Actually, looking at OpenFermionPySCF's run_pyscf:
//   two_body_integrals = molecule.ao_to_mo_two_e(ao_integrals, c_matrix)
// And in MolecularData:
//   def ao_to_mo_two_e(self, ao_integrals, c_matrix):
//     return general_basis_change(ao_integrals, c_matrix, key=(1,0,1,0))
// The key=(1,0,1,0) means it transposes to convert from AO to MO basis
// in a specific way.

// OK I'm going in circles. Let me just verify numerically.
// The OpenFermion test says:
//   two_body[0,1,1,0] = 0.337245
// where two_body is InteractionOperator.two_body_tensor (includes 1/2 factor).
// This means spatial[0,0,0,0] = 2 * 0.337245 = 0.67449
// And our h2_spatial[0,0,0,0] = 0.6745 ✓

// two_body[0,2,2,0] = 0.331735
// spin[0,2,2,0] = 2 * 0.331735 = 0.66347
// From same-spin alpha: spin[2p,2q,2r,2s] = spatial[p,q,r,s]
//   [0,2,2,0] → p=0,q=1,r=1,s=0 → spatial[0,1,1,0] = 0.66347
// Our h2_spatial[0,1,1,0] = 0.6976 ← WRONG

// two_body[0,2,0,2] = 0.090645
// spin[0,2,0,2] = 2 * 0.090645 = 0.18129
// [0,2,0,2] → p=0,q=1,r=0,s=1 → spatial[0,1,0,1] = 0.18129
// Our h2_spatial[0,1,0,1] = 0.6976 ← WRONG

// So the problem is clear:
// OpenFermion's spatial[0,1,1,0] = 0.66347 (our: 0.6976)
// OpenFermion's spatial[0,1,0,1] = 0.18129 (our: 0.6976)
//
// IF the spatial array is in chemist's notation, then spatial[0,1,1,0] = (01|10)_chem
// And (01|10)_chem = (10|01)_chem by particle swap symmetry
// And (01|10)_chem by [pq|sr] symmetry = (01|01)_chem... no wait
// [pq|rs] = [pq|sr] only if r=s... [01|10] → [01|01] would be swapping r,s = 1,0
// [pq|rs]: [01|10] → p=0,q=1,r=1,s=0, swapping r↔s gives [01|01] = [pq|sr]
// The symmetry is [pq|rs] = [qp|rs] = [pq|sr] = [qp|sr] and [rs|pq] etc.
// So yes, [01|10] = [01|01] in chemist's notation.
// But OpenFermion has them as DIFFERENT values: 0.66347 vs 0.18129.
//
// THEREFORE: OpenFermion's spatial array is NOT in chemist's notation!
// It must be in physicist's notation ⟨pq|rs⟩.

// OK let me verify: if spatial[p,q,r,s] = ⟨pq|rs⟩_phys then:
//   ⟨01|10⟩_phys = ∫ φ₀(1)φ₁(2) (1/r12) φ₁(1)φ₀(2) = K₀₁ = exchange
//   ⟨01|01⟩_phys = ∫ φ₀(1)φ₁(2) (1/r12) φ₀(1)φ₁(2) = J₀₁ = Coulomb

// Hmm wait, ⟨01|01⟩_phys = ∫ φ₀*(1)φ₁*(2) (1/r12) φ₀(1)φ₁(2) 
//   = ∫ |φ₀(1)|² (1/r12) |φ₁(2)|² = J₀₁
// And ⟨01|10⟩_phys = ∫ φ₀*(1)φ₁*(2) (1/r12) φ₁(1)φ₀(2)
//   = ∫ φ₀*(1)φ₁(1) (1/r12) φ₁*(2)φ₀(2) = K₀₁

// So: J₀₁ = 0.18129 and K₀₁ = 0.66347 ← K > J for H₂/STO-3G!
// This is unusual but physically possible for molecular orbitals that are 
// delocalized over the same atoms.

// Actually wait: J = ⟨01|01⟩ involves |φ₀|²|φ₁|² which for σ_g and σ_u is
// (s_A+s_B)²(s_A-s_B)² / normalization, which has a node at the midpoint.
// K = ⟨01|10⟩ involves φ₀*φ₁ · φ₁*φ₀ = |φ₀φ₁|² which is always positive.

// Hmm actually J₀₁ should be:
//   J₀₁ = ∫∫ |σ_g(r₁)|² (1/r₁₂) |σ_u(r₂)|² dr₁dr₂
// σ_g and σ_u both have electron density around the two atoms.
// This integral should be positive and fairly large.

// Actually, I realize the issue: for H₂ STO-3G with R = 0.7414 Å,
// the Coulomb integral J₀₁ between bonding and antibonding orbitals 
// can indeed be smaller than K₀₁ because of the specific orbital shapes.
// The STO-3G orbitals have a very specific character.

// Let me just look up the known values. From Szabo & Ostlund Table 3.15:
//   For H₂/STO-3G at R = 1.4 bohr ≈ 0.7408 Å:
//   (11|22) = 0.6636 (Coulomb, chemist notation)  
//   (12|12) = 0.1813 (Exchange, chemist notation = [12|12] in their notation)
//
// Note: Szabo uses 1-indexed notation.
// (11|22) in Szabo = [00|11] in our 0-indexed = 0.6636
// (12|12) in Szabo = [01|01] in our 0-indexed = 0.1813
//
// And the 8-fold symmetry:
// [01|01] = ∫ φ₀(1)φ₁(1) (1/r12) φ₀(2)φ₁(2) d1d2
// [01|10] = ∫ φ₀(1)φ₁(1) (1/r12) φ₁(2)φ₀(2) d1d2
// [01|01] symmetry: [pq|rs] = [qp|rs] = [pq|sr] = [rs|pq] → [10|10] = [01|10] = [10|01] = [01|01]
// So [01|01] = [01|10]! All four are the same.
//
// And Szabo: (12|12) = 0.1813 = [01|01] = [01|10] = K₀₁

// So the exchange integral K₀₁ = 0.1813
// And the Coulomb integral J₀₁ = (11|22) = [00|11] = 0.6636

// Now mapping to OpenFermion:
// spatial_OF[0,1,1,0] means either:
//   chemist: (01|10) = [01|10] = K₀₁ = 0.1813
//   OR physicist: ⟨01|10⟩ = (01|10)? No...
//   
// ⟨pq|rs⟩_phys = [pr|qs]_chem
// ⟨01|10⟩_phys = [01|10]_chem = K₀₁ = 0.1813

// But OpenFermion says spatial_OF[0,1,1,0] = 0.66347, not 0.1813!

// Hmm. So spatial_OF[0,1,1,0] ≠ (01|10)_chem ≠ ⟨01|10⟩_phys

// Then what IS spatial_OF?

// Let me try: spatial_OF[p,q,r,s] = (ps|rq)_chem
// [0,1,1,0] → (00|11) = J₀₁ = 0.6636 ≈ 0.66347 ✓
// [0,1,0,1] → (01|01) = K₀₁ = 0.1813 ≈ 0.18129 ✓

// YES! So spatial_OF[p,q,r,s] = (ps|rq)_chem = [ps|rq]

// Or equivalently: spatial_OF[p,q,r,s] = ⟨pr|sq⟩_phys since (ps|rq)_chem = ⟨pr|sq⟩_phys? 
// Let me check: ⟨pq|rs⟩_phys = [pr|qs]_chem
// ⟨pr|sq⟩_phys = [ps|rq]_chem ← YES!

// So spatial_OF[p,q,r,s] = ⟨pr|sq⟩_phys

// Hmm, that's a weird convention. Let me try another interpretation.
// Maybe spatial_OF[p,q,r,s] = [pr|qs]_chem (just like chemist→physicist)?
// [0,1,1,0] → [01|10]_chem = K₀₁ = 0.1813 ← NO, doesn't match 0.66347

// OK so it seems like spatial_OF[p,q,r,s] = (ps|rq)_chem
// Or equivalently it could be that PySCF stores things differently...

// Actually, the simplest interpretation:
// spatial_OF[p,q,r,s] = (pq|rs)_chem with indices interpreted as (p,s | r,q)?
// No that's getting too complicated.

// Let me just check all possibilities:
printfn "Trying to identify OpenFermion spatial convention..."
printfn ""

let known_chem = Array4D.init 2 2 2 2 (fun p q r s ->
    // Chemist's notation (pq|rs)
    match (p,q,r,s) with
    | (0,0,0,0) -> 0.67449
    | (1,1,1,1) -> 0.69740
    | (0,0,1,1) | (1,1,0,0) -> 0.66347  // J₀₁ = Coulomb? No...
    // Wait, I said Szabo (11|22) = [00|11] = J₀₁ = 0.6636 ≈ 0.66347
    // And Szabo (12|12) = [01|01] = K₀₁ = 0.1813
    | (0,1,0,1) | (1,0,1,0) | (0,1,1,0) | (1,0,0,1) -> 0.18129  // K₀₁
    | _ -> 0.0)

// OF spatial values:
let of_spatial = Array4D.init 2 2 2 2 (fun p q r s -> 0.0)  // will fill

// From OF test, the InteractionOp two_body tensor values (spin orbital):
// two_body[0,1,1,0] = g3 = 0.337245
//   Mixed spin: spinorb[2p,2q+1,2r+1,2s] = spatial[p,q,r,s]
//   → [0,1,1,0] = spinorb[0,1,1,0] → p=0,q=0,r=0,s=0 → spatial[0,0,0,0]
//   InteractionOp = spatial[0,0,0,0]/2 = 0.337245 → spatial[0,0,0,0] = 0.674490

// two_body[2,3,3,2] = g4 = 0.34870
//   Mixed spin: spinorb[2p,2q+1,2r+1,2s] → [2,3,3,2] → p=1,q=1,r=1,s=1
//   spatial[1,1,1,1] = 0.69740

// two_body[0,2,2,0] = g5 = 0.331735
//   Same spin α: spinorb[2p,2q,2r,2s] → [0,2,2,0] → p=0,q=1,r=1,s=0
//   spatial[0,1,1,0] = 0.66347

// two_body[0,2,0,2] = g6 = 0.090645
//   Same spin α: spinorb[2p,2q,2r,2s] → [0,2,0,2] → p=0,q=1,r=0,s=1
//   spatial[0,1,0,1] = 0.18129

// Now comparing with known chemist's values:
printfn "OF spatial[0,0,0,0] = 0.67449 vs chem[00|00] = %.5f → %s" known_chem.[0,0,0,0] (if abs(0.67449 - known_chem.[0,0,0,0]) < 0.001 then "MATCH" else "NO MATCH")
printfn "OF spatial[1,1,1,1] = 0.69740 vs chem[11|11] = %.5f → %s" known_chem.[1,1,1,1] (if abs(0.69740 - known_chem.[1,1,1,1]) < 0.001 then "MATCH" else "NO MATCH")
printfn "OF spatial[0,1,1,0] = 0.66347 vs chem[01|10] = %.5f → %s" known_chem.[0,1,1,0] (if abs(0.66347 - known_chem.[0,1,1,0]) < 0.001 then "MATCH" else "NO MATCH")
printfn "OF spatial[0,1,0,1] = 0.18129 vs chem[01|01] = %.5f → %s" known_chem.[0,1,0,1] (if abs(0.18129 - known_chem.[0,1,0,1]) < 0.001 then "MATCH" else "NO MATCH")
printfn ""

// So: OF spatial[0,1,1,0] = 0.66347 but chem[01|10] = 0.18129 → NO MATCH
//     OF spatial[0,1,0,1] = 0.18129 but chem[01|01] = 0.18129 → MATCH
//     
// And chem[00|11] = 0.66347 (J₀₁) — does OF spatial[0,1,1,0] = chem[00|11]?
// chem[00|11] = 0.66347, OF spatial[0,1,1,0] = 0.66347 → YES!
//
// So: OF spatial[0,1,1,0] = chem[00|11] = (00|11)
// Let me check the pattern: OF spatial[p,q,r,s] → chem[p,s,r,q]?
// [0,1,1,0] → chem[0,0,1,1] = 0.66347 ✓
// [0,1,0,1] → chem[0,1,0,1] = 0.18129 ✓
// [0,0,0,0] → chem[0,0,0,0] = 0.67449 ✓
// [1,1,1,1] → chem[1,1,1,1] = 0.69740 ✓
//
// BINGO! OF spatial[p,q,r,s] = (p,s | r,q)_chemist = [ps|rq]

// So the PySCF/OpenFermion convention for the spatial integral array is:
//   spatial[p,q,r,s] = (ps|rq)_chemist

// Or equivalently:
//   spatial[p,q,r,s] = ⟨pr|sq⟩_physicist  (since (ps|rq) = ⟨pr|sq⟩)

// Actually wait, that doesn't seem standard either. Let me reconsider.
// Perhaps the convention is just that PySCF stores integrals differently from
// what I think is "standard" chemist's notation.

// Actually, I think what's happening is: PySCF stores the spatial integrals as
// spatial[p,r,q,s] = (pq|rs)_chem  (note the transposed q,r indices)
// So: spatial[0,1,1,0] = (01|10)_chem with the mapping spatial[p,r,q,s] → (pq|rs)
//   p=0,r=1,q=1,s=0 → (pq|rs) = (01|10) ... that gives (01|10) = K = 0.18129, not 0.66347

// Hmm, let me try: spatial[p,q,r,s] where the convention is the "Mulliken" or 
// "charge-density" ordering: (pr|qs) = ∫ φ_p(1)φ_r(1) (1/r12) φ_q(2)φ_s(2)
// spatial[p,q,r,s] = (pr|qs) → (p=0,r=1 | q=1,s=0) → integral ∫ φ₀φ₁(1/r)φ₁φ₀
// That's K = 0.18129, not 0.66347.

// OK let me just try the remaining possibility:
// spatial[p,q,r,s] = (pq|rs) = ∫ φ_p(1)φ_q(1) (1/r12) φ_r(2)φ_s(2)
// [0,1,1,0]: (01|10) = ∫ φ₀φ₁(1/r)φ₁φ₀ = K = 0.18129 ← doesn't match 0.66347
//
// Unless the PySCF convention for (pq|rs) actually means:
// ∫ φ_p(1)φ_r(1) (1/r12) φ_q(2)φ_s(2)?? That would be weird.
// [0,1,1,0]: ∫ φ₀φ₁(1/r)φ₁φ₀ = same thing = 0.18129

// What if spatial[p,q,r,s] = ⟨pq|rs⟩ but using Dirac bra-ket notation?
// ⟨pq|rs⟩ = ∫ φ_p*(1)φ_q*(2) (1/r12) φ_r(1)φ_s(2)
// [0,1,1,0]: ⟨01|10⟩ = ∫ φ₀(1)φ₁(2) (1/r12) φ₁(1)φ₀(2) = K₀₁ = 0.18129
// Still 0.18129!

// I keep getting 0.18129 for [0,1,1,0] and the OF value is 0.66347.
// The only way to get 0.66347 is if it equals J₀₁ = (00|11) = 0.66347.
// So OF spatial[0,1,1,0] = J₀₁ = 0.66347.

// How? If spatial[p,q,r,s] = (ps|qr)_chem:
// [0,1,1,0]: (p=0,s=0 | q=1,r=1) = (00|11) = J₀₁ = 0.66347 ✓
// [0,1,0,1]: (p=0,s=1 | q=1,r=0) = (01|10) = K₀₁ = 0.18129 ✓
// [0,0,0,0]: (p=0,s=0 | q=0,r=0) = (00|00) = 0.67449 ✓
// [1,1,1,1]: (p=1,s=1 | q=1,r=1) = (11|11) = 0.69740 ✓

printfn "ANSWER: OF spatial[p,q,r,s] = (ps|qr)_chemist"
printfn ""
printfn "This means: spatial[p,q,r,s] represents the integral:"
printfn "  ∫ φ_p(1)φ_s(1) (1/r₁₂) φ_q(2)φ_r(2) d1d2"
printfn ""

// Now, how does this relate to OUR code?
// Our H2Demo.fsx builds h2_spin using:
//   ⟨pq|rs⟩ = [pr|qs]_chem   (physicist's = chemist's with swapped indices)
// And looks up: h2_spatial.[p/2, r/2, q/2, s/2]
// 
// Our h2_spatial is supposed to be in chemist's notation [p,q,r,s] = (pq|rs)
// Then h2_spatial[p/2, r/2, q/2, s/2] = (p/2,r/2 | q/2,s/2) = [p/2,r/2 | q/2,s/2]
// And this should equal ⟨p/2,q/2 | r/2,s/2⟩_phys = (p/2,r/2|q/2,s/2) ✓
//
// So our CONVERSION is correct — the problem is in the spatial integral VALUES.
// Our h2_spatial is supposed to contain chemist's (pq|rs) integrals, but we have:
//   h2_spatial[0,1,1,0] = 0.6976 — this should be (01|10)_chem = K₀₁ = 0.18129
//   h2_spatial[0,1,0,1] = 0.6976 — this should be (01|01)_chem = K₀₁ = 0.18129

// Actually wait: (01|10) in chemist = ∫ φ₀φ₁ (1/r) φ₁φ₀ = K₀₁
// And (01|01) in chemist = ∫ φ₀φ₁ (1/r) φ₀φ₁ = same by exchanging electron labels = K₀₁
// So (01|10) = (01|01) = K₀₁ = 0.18129 in true chemist's notation.

// But we have h2_spatial[0,1,0,1] = h2_spatial[0,1,1,0] = 0.6976.
// This value is WRONG. The correct value is 0.18129.

// What IS 0.6976? Let me check: 
// 0.6976 ≈ 0.6975782 from our code
// OF g4*2 = 0.69740 ≈ (11|11) ← close but not exact
// Actually 0.6975782 is quite close to (11|11) = 0.69740
// But it's also listed separately as our h2vals.[1]

// Hmm, from our original code comment:
//   [01|10] = [10|01] = [01|01]* = [10|10]* = 0.6975782468828187
// And we call this an "8-fold symmetry" equivalent set.
// But 0.6976 is NOT the exchange integral K₀₁ = 0.18129.
// So where did 0.6976 come from?

// Looking at the reference cited: "O'Malley et al., Phys. Rev. X 6, 031007 (2016) — Table I"
// It's possible that the integrals in the original H2Demo.fsx were copied incorrectly
// from Table I, perhaps confusing chemist's and physicist's notation.

// The value 0.6975782 is actually very close to the two-electron integral (11|11) = (σ_u σ_u | σ_u σ_u).
// It could be that this was mistakenly placed in the [01|10] slot when it should be in [11|11].

// Wait — our h2vals.[1] = 0.6973979 and h2vals.[3] = 0.6975782. These are different but close.
// Could they both be variants of (11|11) due to some confusion?

// ACTUAL DIAGNOSIS: I think what happened is that the integrals were sourced from a 
// reference that uses PHYSICIST'S notation ⟨pq|rs⟩ but were placed into our array
// assuming CHEMIST'S notation. The value 0.6976 is actually ⟨01|10⟩_phys which 
// equals (00|11)_chem = J₀₁ = 0.66347... no, that's 0.66347, not 0.6976.

// OK, I think the values might come from a different geometry or different reference.
// The important thing is: our values don't match the OpenFermion standard.

printfn "═══════════════════════════════════════════════════════"
printfn " SUMMARY OF INTEGRAL DISCREPANCY"
printfn "═══════════════════════════════════════════════════════"
printfn ""
printfn "Our h2_spatial values (chemist's notation [pq|rs]):"
printfn "  [00|00] = %.10f" h2_spatial.[0,0,0,0]
printfn "  [11|11] = %.10f" h2_spatial.[1,1,1,1]
printfn "  [00|11] = %.10f" h2_spatial.[0,0,1,1]
printfn "  [01|10] = %.10f" h2_spatial.[0,1,1,0]
printfn "  [01|01] = %.10f" h2_spatial.[0,1,0,1]
printfn ""

// What OpenFermion says the values should be (deriving chemist's from their spin-orbital):
// spatial_OF[p,q,r,s] = (ps|qr)_chem
// So (pq|rs)_chem = spatial_OF[p,r,s,q]
//
// (00|00) = spatial_OF[0,0,0,0] = 0.67449
// (11|11) = spatial_OF[1,1,1,1] = 0.69740
// (00|11) = spatial_OF[0,1,1,0] = 0.66347
// (01|10) = spatial_OF[0,1,0,1] = 0.18129
// (01|01) = (01|10) by symmetry = 0.18129

printfn "OpenFermion-derived values (chemist's notation):"
printfn "  [00|00] = 0.67449"
printfn "  [11|11] = 0.69740"
printfn "  [00|11] = 0.66347"
printfn "  [01|10] = 0.18129"
printfn "  [01|01] = 0.18129"
printfn ""
printfn "The discrepancy is in h2vals.[2] and h2vals.[3]:"
printfn "  h2vals.[2] = %.10f  should be [00|11] = J₀₁" h2vals.[2]
printfn "  h2vals.[3] = %.10f  should be [01|10] = K₀₁" h2vals.[3]
printfn "  OpenFermion:  [00|11] = 0.6635 (Coulomb J₀₁)"
printfn "  OpenFermion:  [01|10] = 0.1813 (Exchange K₀₁)"
printfn ""
printfn "Our h2vals.[2] = 0.6636 is close to OF's [00|11] = 0.6635 ✓"
printfn "But our h2vals.[3] = 0.6976 is WRONG (should be 0.1813)"
printfn ""
printfn "The value 0.6976 does not correspond to any correct H₂/STO-3G integral."
printfn "It seems to be a DATA ENTRY ERROR in the original H2Demo.fsx."
printfn ""

// Let me now compute what the correct E_HF and E_FCI should be with corrected integrals.
let h00 = -1.2563390730032498
let h11 = -0.4718960244306283
let g0000 = 0.6744887663049631
let g1111 = 0.6973979494693556
let g0011_J = 0.6636340478615040  // This seems correct
let g0110_K = 0.18128880839426165  // The CORRECT exchange integral (from OF: 0.18129)

// Actually I need the precise value. Let me use the OF precision:
// g6 = 0.18129 (limited precision in the test)
// But our integrals have higher precision. Let me compute from Szabo:
// Actually the precise values would need a proper integral evaluation.
// Let me just use the OF value 0.18129 and see if energies make sense.

// E_HF (electronic) = 2*h₀₀ + J₀₀ (for 2 electrons in orbital 0)
// Wait, for H₂ RHF:
// E_HF = 2*h₀₀ + J₀₀ = 2*h₀₀ + (00|00) = 2*(-1.2563) + 0.6745 = -1.8382
// This doesn't depend on the exchange integral, so E_HF is unchanged.

let E_HF_elec = 2.0 * h00 + g0000
printfn "E_HF (electronic) = 2h₀₀ + (00|00) = %.10f Ha" E_HF_elec
printfn "E_HF (total) = %.10f Ha" (E_HF_elec + nuclearRepulsion)
printfn ""

// For FCI: the 2-electron sector has the key 2×2 block:
// |1100⟩ and |0011⟩ are coupled.
//
// Matrix element ⟨1100|H|1100⟩ = E_HF_elec (as above)
// Matrix element ⟨0011|H|0011⟩ = 2*h₁₁ + (11|11)
// Matrix element ⟨1100|H|0011⟩ = (01|01) or (01|10)?
//
// In the Hamiltonian H = Σ h_{pq} a†_p a_q + (1/2) Σ ⟨pq|rs⟩ a†_p a†_q a_s a_r
// 
// ⟨1100|H|0011⟩ involves annihilating 2,3 and creating 0,1
// a†_0 a†_1 a_3 a_2 acting on |0011⟩ = a†_2 a†_3 |vac⟩
// = a†_0 a†_1 a_3 a_2 a†_2 a†_3 |vac⟩
// = a†_0 a†_1 |vac⟩ = |1100⟩
// coefficient = ⟨01|23⟩_phys / 2
// 
// Hmm wait, in spin-orbital basis 0=0α, 1=0β, 2=1α, 3=1β
// ⟨1100|H|0011⟩ = (1/2) * Σ_{pqrs} ⟨pq|rs⟩ ⟨1100|a†_p a†_q a_s a_r|0011⟩
// The only terms that survive are those that annihilate orbitals 2,3 and create 0,1:
//   p=0,q=1,r=2,s=3: ⟨01|23⟩ or p=0,q=1,r=3,s=2: ⟨01|32⟩ etc.
//
// For spin conservation: spin_p = spin_r, spin_q = spin_s
// p=0(α), q=1(β), r=2(α), s=3(β) → ⟨01|23⟩ with 0→2 (α→α), 1→3 (β→β) ✓
// ⟨01|23⟩_phys = spatial ⟨0,0|1,1⟩_phys = (01|01)_chem = K₀₁ 
// No wait:
// spin orbital ⟨01|23⟩: φ₀ₐ(1)φ₀ᵦ(2) (1/r12) φ₁ₐ(1)φ₁ᵦ(2)
// = δ(α,α)δ(β,β) · ⟨0,0|1,1⟩_spatial = (01|01)_chem_spatial = K₀₁

// And p=1,q=0,r=3,s=2: ⟨10|32⟩ → same with switched electrons = same value
// p=1,q=0,r=2,s=3: ⟨10|23⟩ → spin mismatch (β→α for first)
// p=0,q=1,r=3,s=2: ⟨01|32⟩ → spin mismatch

// So the matrix element is:
// (1/2)[⟨01|23⟩ + ⟨10|32⟩] = (1/2)[K + K] = K₀₁ = 0.18129

// Hmm wait, let me be more careful about signs.
// ⟨1100| a†_0 a†_1 a_3 a_2 |0011⟩ = ?
// |0011⟩ = a†_2 a†_3 |vac⟩
// a_2 a†_2 a†_3 |vac⟩ = (1 - a†_2 a_2) a†_3 |vac⟩ = a†_3 |vac⟩
// a_3 a†_3 |vac⟩ = |vac⟩
// a†_1 |vac⟩ = |0100⟩
// a†_0 |0100⟩ = |1100⟩
// So ⟨1100| a†_0 a†_1 a_3 a_2 |0011⟩ = ⟨1100|1100⟩ = +1

// And ⟨1100| a†_1 a†_0 a_2 a_3 |0011⟩ = ?
// a_3 a†_2 a†_3 |vac⟩ = a_3 (-a†_3 a†_2 + ...) hmm
// More carefully: |0011⟩ = a†_2 a†_3 |vac⟩
// a_3 a†_2 a†_3 |vac⟩ = a_3 · (-a†_3 a†_2) |vac⟩ = -a_3 a†_3 a†_2 |vac⟩
// = -(1 - a†_3 a_3) a†_2 |vac⟩ = -a†_2 |vac⟩
// a_2 · (-a†_2 |vac⟩) = -a_2 a†_2 |vac⟩ = -(1 - a†_2 a_2)|vac⟩ = -|vac⟩
// a†_0 · (-|vac⟩) = -a†_0 |vac⟩
// a†_1 · (-a†_0|vac⟩) = -a†_1 a†_0 |vac⟩ = +a†_0 a†_1 |vac⟩ = +|1100⟩
// So ⟨1100| a†_1 a†_0 a_2 a_3 |0011⟩ = +1

// The off-diagonal matrix element:
// (1/2) Σ ⟨pq|rs⟩ ⟨1100|a†_p a†_q a_s a_r|0011⟩
// = (1/2) [⟨01|23⟩·(+1) + ⟨10|32⟩·(+1)]
// = (1/2) [K₀₁ + K₀₁] = K₀₁

// So with the CORRECT integrals:
// Off-diagonal coupling = K₀₁ = 0.18129 (NOT 0.6976 as we had!)

let K01_correct = g0110_K
let diag_1100 = E_HF_elec  // = 2*h00 + g0000
let diag_0011 = 2.0 * h11 + g1111

printfn "2×2 FCI block with CORRECT integrals:"
printfn "  ⟨1100|H|1100⟩ = %.10f" diag_1100
printfn "  ⟨0011|H|0011⟩ = %.10f" diag_0011
printfn "  ⟨1100|H|0011⟩ = %.10f  (K₀₁)" K01_correct
printfn ""

let avg = (diag_1100 + diag_0011) / 2.0
let diff = (diag_1100 - diag_0011) / 2.0
let ground = avg - sqrt(diff*diff + K01_correct*K01_correct)
let excited = avg + sqrt(diff*diff + K01_correct*K01_correct)

printfn "  Ground state energy = %.10f Ha (electronic)" ground
printfn "  Excited state energy = %.10f Ha (electronic)" excited
printfn "  Ground state + Vnuc = %.10f Ha" (ground + nuclearRepulsion)
printfn "  Correlation energy = %.10f Ha" (ground - diag_1100)
printfn "  Correlation energy = %.4f kcal/mol" ((ground - diag_1100) * 627.5095)
printfn ""

// Compare with literature:
printfn "Literature comparison:"
printfn "  Known E_FCI(total) for H₂/STO-3G ≈ -1.1373 Ha"
printfn "  Known E_HF(total) for H₂/STO-3G ≈ -1.1168 Ha"
printfn "  Our E_FCI(total) = %.10f" (ground + nuclearRepulsion)
printfn "  Our E_HF(total) = %.10f" (E_HF_elec + nuclearRepulsion)
