// =============================================================================
//  Custom_Tree.fsx — Build a custom tree encoding
// =============================================================================
// Tree-based encodings map fermionic modes to qubits using a rooted tree.
// ANY tree with n nodes defines a valid encoding for n modes!
//
// Key insight: the tree shape determines the Pauli weight (number of
// non-identity Paulis per operator):
//   - Linear chain (JW):        O(n) weight
//   - Balanced binary tree:     O(log₂ n) weight
//   - Balanced ternary tree:    O(log₃ n) weight (optimal)
//   - Unbalanced trees:         worse scaling
// =============================================================================

#r "../src/Encodings/bin/Debug/net10.0/Encodings.dll"

open Encodings
open System.Numerics

// Helper: count non-identity Paulis in a register
let pauliWeight (reg : PauliRegister) =
    reg.Signature |> Seq.sumBy (fun c -> if c = 'I' then 0 else 1)

let maxWeight (prs : PauliRegisterSequence) =
    prs.SummandTerms |> Array.map pauliWeight |> Array.max

// =============================================================================
//  Build a custom unbalanced tree for 8 modes
// =============================================================================
// We'll make a "right-leaning" tree (almost a chain on one side):
//
//        4 (root)
//       / \
//      2   5
//     / \   \
//    1   3   6
//   /         \
//  0           7

let node0 = { Index = 0; Children = []; Parent = Some 1 }
let node1 = { Index = 1; Children = [node0]; Parent = Some 2 }
let node3 = { Index = 3; Children = []; Parent = Some 2 }
let node2 = { Index = 2; Children = [node1; node3]; Parent = Some 4 }
let node7 = { Index = 7; Children = []; Parent = Some 6 }
let node6 = { Index = 6; Children = [node7]; Parent = Some 5 }
let node5 = { Index = 5; Children = [node6]; Parent = Some 4 }
let node4 = { Index = 4; Children = [node2; node5]; Parent = None }  // root

// Collect all nodes into a map
let rec collectNodes (node : TreeNode) : Map<int, TreeNode> =
    node.Children
    |> List.fold (fun acc child -> Map.fold (fun m k v -> Map.add k v m) acc (collectNodes child))
       (Map.ofList [(node.Index, node)])

let customTree : EncodingTree =
    { Root = node4; Nodes = collectNodes node4; Size = 8 }

// =============================================================================
//  Compare encodings on operator a†_j for all modes
// =============================================================================
let n = 8u
let balanced = balancedBinaryTree 8
let ternary = balancedTernaryTree 8

printfn "Pauli weight comparison for a†_j (8 modes):"
printfn "%-6s  %10s  %10s  %10s" "Mode" "Unbalanced" "Binary" "Ternary"
printfn "%s" (String.replicate 44 "-")

for j in 0u .. n - 1u do
    let customEnc  = encodeWithTernaryTree customTree Raise j n |> maxWeight
    let balancedEnc = encodeWithTernaryTree balanced Raise j n |> maxWeight
    let ternaryEnc = encodeWithTernaryTree ternary Raise j n |> maxWeight
    printfn "  %d       %4d        %4d        %4d" j customEnc balancedEnc ternaryEnc

// =============================================================================
//  Summary: Tree shape matters!
// =============================================================================
printfn ""
printfn "Notice: Unbalanced trees have higher weight for deep modes (0, 7)."
printfn "Balanced trees keep ALL operator weights bounded by O(log n)."
printfn ""
printfn "The ternary tree achieves the theoretical optimum: O(log₃ n) weight."
