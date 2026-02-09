/// AnticommutationTest.fsx — Full CAR verification for all encodings
///
/// For each encoding, builds the 2ⁿ × 2ⁿ matrix representations of all
/// Majorana operators c_j and d_j, then verifies:
///   {c_j, c_k} = 2δ_{jk} · I
///   {d_j, d_k} = 2δ_{jk} · I
///   {c_j, d_k} = 0           ∀ j,k
///
/// Also demonstrates the FAILURE of the index-set construction on
/// non-monotonic trees (balanced binary and ternary trees).

#r "../../Encodings/bin/Debug/net8.0/Encodings.dll"

open System
open System.Numerics
open Encodings

// ═══════════════════════════════════════════════════════
//  Matrix infrastructure (reuse from MatrixVerification)
// ═══════════════════════════════════════════════════════

type CMatrix =
    { Rows : int; Cols : int; Data : Complex[] }
    member m.Item(r, c) = m.Data.[r * m.Cols + c]
    static member Zero(n) = { Rows = n; Cols = n; Data = Array.create (n*n) Complex.Zero }
    static member Identity(n) =
        let d = Array.create (n*n) Complex.Zero
        for i in 0 .. n-1 do d.[i*n + i] <- Complex.One
        { Rows = n; Cols = n; Data = d }
    static member (+) (a : CMatrix, b : CMatrix) =
        { a with Data = Array.init (a.Rows * a.Cols) (fun i -> a.Data.[i] + b.Data.[i]) }
    static member (-) (a : CMatrix, b : CMatrix) =
        { a with Data = Array.init (a.Rows * a.Cols) (fun i -> a.Data.[i] - b.Data.[i]) }
    static member (*) (s : Complex, a : CMatrix) =
        { a with Data = a.Data |> Array.map (fun x -> s * x) }

/// Matrix multiplication
let matMul (a : CMatrix) (b : CMatrix) : CMatrix =
    let n = a.Rows
    let d = Array.create (n * n) Complex.Zero
    for i in 0 .. n-1 do
        for j in 0 .. n-1 do
            let mutable s = Complex.Zero
            for k in 0 .. n-1 do
                s <- s + a.[i, k] * b.[k, j]
            d.[i * n + j] <- s
    { Rows = n; Cols = n; Data = d }

/// Anti-commutator {A, B} = AB + BA
let anticommutator (a : CMatrix) (b : CMatrix) = matMul a b + matMul b a

/// Frobenius norm of a matrix
let frobNorm (m : CMatrix) =
    m.Data |> Array.sumBy (fun x -> x.Real * x.Real + x.Imaginary * x.Imaginary) |> sqrt

let kron (a : CMatrix) (b : CMatrix) : CMatrix =
    let m = a.Rows * b.Rows
    let n = a.Cols * b.Cols
    let d = Array.create (m * n) Complex.Zero
    for ar in 0 .. a.Rows-1 do
        for ac in 0 .. a.Cols-1 do
            let aval = a.[ar, ac]
            if aval <> Complex.Zero then
                for br in 0 .. b.Rows-1 do
                    for bc in 0 .. b.Cols-1 do
                        d.[(ar * b.Rows + br) * n + (ac * b.Cols + bc)] <- aval * b.[br, bc]
    { Rows = m; Cols = n; Data = d }

let pauliI = { Rows = 2; Cols = 2; Data = [| Complex.One; Complex.Zero; Complex.Zero; Complex.One |] }
let pauliX = { Rows = 2; Cols = 2; Data = [| Complex.Zero; Complex.One; Complex.One; Complex.Zero |] }
let pauliY = { Rows = 2; Cols = 2; Data = [| Complex.Zero; Complex(0.,-1.); Complex(0.,1.); Complex.Zero |] }
let pauliZ = { Rows = 2; Cols = 2; Data = [| Complex.One; Complex.Zero; Complex.Zero; Complex.MinusOne |] }

let pauliToMatrix = function Pauli.I -> pauliI | Pauli.X -> pauliX | Pauli.Y -> pauliY | Pauli.Z -> pauliZ

let pauliStringToMatrix (reg : PauliRegister) : CMatrix =
    let n = reg.Signature.Length
    let mutable mat = pauliToMatrix (reg.[0] |> Option.defaultValue Pauli.I)
    for i in 1 .. n-1 do
        mat <- kron mat (pauliToMatrix (reg.[i] |> Option.defaultValue Pauli.I))
    reg.Coefficient * mat

let seqToMatrix (prs : PauliRegisterSequence) (nq : int) : CMatrix =
    let dim = 1 <<< nq
    let mutable result = CMatrix.Zero dim
    for term in prs.SummandTerms do
        result <- result + pauliStringToMatrix term
    result

// ═══════════════════════════════════════════════════════
//  Build Majorana matrices from encoding
// ═══════════════════════════════════════════════════════

/// Get the c and d Majorana operators as matrices for mode j.
///   a†_j = ½(c_j - i·d_j)
///   a_j  = ½(c_j + i·d_j)
/// So: c_j = a†_j + a_j,  d_j = i(a†_j - a_j)
let majoranaMatrices (encode : EncoderFn) (j : int) (n : int) : CMatrix * CMatrix =
    let nq = n
    let ju = uint32 j
    let nu = uint32 n
    let adag = seqToMatrix (encode Raise ju nu) nq
    let a    = seqToMatrix (encode Lower ju nu) nq
    let cj = adag + a                          // c_j = a† + a
    let dj = Complex(0.0, 1.0) * (adag - a)   // d_j = i(a† - a)
    (cj, dj)

// ═══════════════════════════════════════════════════════
//  CAR verification
// ═══════════════════════════════════════════════════════

/// Verify all anti-commutation relations for n modes.
/// Returns (pass, max_deviation, failure_details).
let verifyCAR (name : string) (encode : EncoderFn) (n : int) =
    let dim = 1 <<< n
    let identity2n = Complex(2.0, 0.0) * CMatrix.Identity dim
    let zero = CMatrix.Zero dim

    // Build all Majorana matrices
    let cs = Array.init n (fun j -> majoranaMatrices encode j n |> fst)
    let ds = Array.init n (fun j -> majoranaMatrices encode j n |> snd)

    let mutable maxDev = 0.0
    let mutable failures = []

    // Check {c_j, c_k} = 2δ_{jk} I
    for j in 0 .. n-1 do
        for k in j .. n-1 do
            let ac = anticommutator cs.[j] cs.[k]
            let expected = if j = k then identity2n else zero
            let dev = frobNorm (ac - expected)
            if dev > maxDev then maxDev <- dev
            if dev > 1e-10 then
                failures <- (sprintf "{c_%d, c_%d} deviation = %.2e" j k dev) :: failures

    // Check {d_j, d_k} = 2δ_{jk} I
    for j in 0 .. n-1 do
        for k in j .. n-1 do
            let ac = anticommutator ds.[j] ds.[k]
            let expected = if j = k then identity2n else zero
            let dev = frobNorm (ac - expected)
            if dev > maxDev then maxDev <- dev
            if dev > 1e-10 then
                failures <- (sprintf "{d_%d, d_%d} deviation = %.2e" j k dev) :: failures

    // Check {c_j, d_k} = 0  ∀ j, k
    for j in 0 .. n-1 do
        for k in 0 .. n-1 do
            let ac = anticommutator cs.[j] ds.[k]
            let dev = frobNorm ac
            if dev > maxDev then maxDev <- dev
            if dev > 1e-10 then
                failures <- (sprintf "{c_%d, d_%d} deviation = %.2e" j k dev) :: failures

    (failures.IsEmpty, maxDev, failures |> List.rev)

// ═══════════════════════════════════════════════════════
//  Index-set construction for arbitrary trees
// ═══════════════════════════════════════════════════════

/// Build an encoding function from a tree using the INDEX-SET method.
/// This WILL FAIL for non-monotonic trees (that's what we want to show).
let indexSetEncoder (tree : EncodingTree) : EncoderFn =
    let scheme = treeEncodingScheme tree
    fun op j n -> encodeOperator scheme op j n

/// Build an encoding function from a tree using the PATH-BASED method.
/// This works for ALL trees.
let pathBasedEncoder (tree : EncodingTree) : EncoderFn =
    fun op j n -> encodeWithTernaryTree tree op j n

// ═══════════════════════════════════════════════════════
//  Run tests
// ═══════════════════════════════════════════════════════

printfn ""
printfn "╔═══════════════════════════════════════════════════════╗"
printfn "║  Anti-Commutation Relation (CAR) Verification        ║"
printfn "║  Tests {c_j, c_k} = 2δ_{jk}I, {d_j, d_k} = 2δ_{jk}I║"
printfn "║  and {c_j, d_k} = 0 for all encodings               ║"
printfn "╚═══════════════════════════════════════════════════════╝"
printfn ""

let testSizes = [4; 6; 8]

// ─── Test standard encodings ───
printfn "━━━ Standard encodings (should ALL pass) ━━━"
printfn ""

for n in testSizes do
    printfn "── n = %d ──" n
    let encodings : (string * EncoderFn) list =
        [ "Jordan-Wigner",     jordanWignerTerms
          "Bravyi-Kitaev",     bravyiKitaevTerms
          "Parity",            parityTerms
          "Balanced Binary (path-based)",  balancedBinaryTreeTerms
          "Balanced Ternary (path-based)", ternaryTreeTerms ]

    for (name, enc) in encodings do
        let (pass, maxDev, failures) = verifyCAR name enc n
        if pass then
            printfn "  ✅ %-38s max deviation = %.2e" name maxDev
        else
            printfn "  ❌ %-38s max deviation = %.2e" name maxDev
            for f in failures |> List.truncate 5 do
                printfn "       %s" f
            if failures.Length > 5 then
                printfn "       ... and %d more failures" (failures.Length - 5)
    printfn ""

// ─── Test index-set construction on non-monotonic trees (should FAIL) ───
printfn "━━━ Index-set construction on non-monotonic trees (should FAIL) ━━━"
printfn ""

for n in [4; 6; 8] do
    printfn "── n = %d ──" n
    let trees =
        [ "Balanced Binary (INDEX-SET)", balancedBinaryTree n
          "Balanced Ternary (INDEX-SET)", balancedTernaryTree n ]

    for (name, tree) in trees do
        let enc = indexSetEncoder tree
        let (pass, maxDev, failures) = verifyCAR name enc n
        if pass then
            printfn "  ✅ %-38s max deviation = %.2e (unexpectedly passed!)" name maxDev
        else
            printfn "  ❌ %-38s max deviation = %.2e (EXPECTED failure)" name maxDev
            for f in failures |> List.truncate 3 do
                printfn "       %s" f
            if failures.Length > 3 then
                printfn "       ... and %d more failures" (failures.Length - 3)
    printfn ""

// ─── Monotonicity check ───
printfn "━━━ Monotonicity check ━━━"
printfn ""

let isMonotonic (tree : EncodingTree) : bool =
    // Monotonic = every node's ancestors have LARGER indices than the node.
    // This is the condition required by the index-set construction's
    // remainder set formula: R(j) filters children with Index < j.
    tree.Nodes |> Map.forall (fun idx node ->
        match node.Parent with
        | None   -> true
        | Some p -> p > idx)

/// Build a Fenwick tree as an EncodingTree (for monotonicity testing).
/// Parent of j (0-indexed) = j + lowestBit(j+1), root when parent >= n.
let fenwickEncodingTree (n : int) : EncodingTree =
    let lowestBit x = x &&& (-x)
    let parentOf j =
        let p = j + lowestBit (j + 1)
        if p >= n then None else Some p
    // Build nodes bottom-up
    let childrenMap =
        [0 .. n-1]
        |> List.choose (fun j -> parentOf j |> Option.map (fun p -> (p, j)))
        |> List.groupBy fst
        |> List.map (fun (p, cs) -> (p, cs |> List.map snd))
        |> Map.ofList
    let rec buildNode j =
        let children = childrenMap |> Map.tryFind j |> Option.defaultValue [] |> List.map buildNode
        { Index = j; Children = children; Parent = parentOf j }
    let root = [0 .. n-1] |> List.find (fun j -> parentOf j = None)
    let rootNode = buildNode root
    // Flatten to node map
    let rec collectNodes (node : TreeNode) =
        (node.Index, node) :: (node.Children |> List.collect collectNodes)
    let nodes = collectNodes rootNode |> Map.ofList
    { Root = rootNode; Nodes = nodes; Size = n }

for n in [4; 8; 16] do
    printfn "  n = %d:" n
    for (name, mkTree) in [ "Linear chain",     fun n -> linearTree n
                            "Fenwick tree (BK)", fun n -> fenwickEncodingTree n
                            "Balanced Binary",   fun n -> balancedBinaryTree n
                            "Balanced Ternary",  fun n -> balancedTernaryTree n ] do
        let tree = mkTree n
        let mono = isMonotonic tree
        let sym = if mono then "✅ monotonic" else "❌ non-monotonic"
        printfn "    %-25s %s" name sym
    printfn ""

// ─── Detailed failure trace for Paper 3 ───
printfn "━━━ Detailed failure trace (n=4, balanced binary, index-set) ━━━"
printfn ""

let n4 = 4
let binTree4 = balancedBinaryTree n4
let binIndexSet = indexSetEncoder binTree4
let binPathBased = pathBasedEncoder binTree4

printfn "  Tree structure (balanced binary, n=4):"
for kvp in binTree4.Nodes |> Map.toSeq do
    let node = snd kvp
    let parent = match node.Parent with Some p -> string p | None -> "root"
    let children = node.Children |> List.map (fun c -> string c.Index) |> String.concat ", "
    printfn "    Node %d: parent=%s, children=[%s]" node.Index parent children
printfn ""

printfn "  Index-set construction:"
let scheme4 = treeEncodingScheme binTree4
for j in 0 .. n4-1 do
    let u = scheme4.Update j n4
    let p = scheme4.Parity j
    let occ = scheme4.Occupation j
    printfn "    j=%d: U=%A  P=%A  Occ=%A" j u p occ
printfn ""

// Show the specific anti-commutation failures
printfn "  Anti-commutation test (index-set vs. path-based):"
let (passIdx, devIdx, failIdx) = verifyCAR "index-set" binIndexSet n4
let (passPath, devPath, _) = verifyCAR "path-based" binPathBased n4
printfn "    Index-set:  %s (max dev = %.2e)" (if passIdx then "PASS" else "FAIL") devIdx
printfn "    Path-based: %s (max dev = %.2e)" (if passPath then "PASS" else "FAIL") devPath
if not passIdx then
    printfn ""
    printfn "  Specific failures (index-set):"
    for f in failIdx do
        printfn "    %s" f

printfn ""
printfn "Done."
