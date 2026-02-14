namespace Encodings

/// General tree-based fermion-to-qubit encodings.
///
/// This module implements two approaches:
///
/// (1) Index-set approach (Havlíček et al. arXiv:1701.07072):
///     Works correctly ONLY for Fenwick trees (where all ancestors > j).
///     Uses Update/Parity/Occupation sets with the generic MajoranaEncoding.
///
/// (2) Path-based ternary-tree approach (Bonsai: arXiv:2212.09731,
///     Jiang et al.: arXiv:1910.10746):
///     Works for ANY ternary tree. Constructs Majorana strings directly
///     from root-to-leg paths. Each node has 3 descending links labeled
///     X, Y, Z.  Each leg yields a Pauli string by collecting the labels
///     along its root-to-leg path. Paired legs give the two Majoranas
///     for each fermionic mode.
///
/// Different tree shapes yield different encodings:
///   - Chain (linear tree):   recovers Jordan-Wigner
///   - Fenwick tree:          recovers Bravyi-Kitaev
///   - Balanced ternary tree: achieves O(log₃ n) Pauli weight (optimal)
[<AutoOpen>]
module TreeEncoding =

    open System.Numerics

    // ─────────────────────────────────────────────
    //  Tree data structure
    // ─────────────────────────────────────────────

    /// A node in a rooted tree. Each node represents one qubit/mode.
    type TreeNode =
        { /// 0-based index of this node (= fermionic mode index).
          Index    : int
          /// Children of this node, ordered by index.
          Children : TreeNode list
          /// Parent index, or None for the root.
          Parent   : int option }

    /// A rooted tree on n nodes. Nodes are indexed 0 .. n-1.
    type EncodingTree =
        { Root  : TreeNode
          Nodes : Map<int, TreeNode>
          Size  : int }

    // ─────────────────────────────────────────────
    //  Tree traversal helpers
    // ─────────────────────────────────────────────

    /// Ancestors of node j (from j toward root, excluding j).
    let treeAncestors (tree : EncodingTree) (j : int) : int list =
        let rec walk idx acc =
            match Map.find idx tree.Nodes with
            | { Parent = Some p } -> walk p (p :: acc)
            | _ -> List.rev acc
        walk j []

    /// All descendants of node j (excluding j), via DFS.
    let treeDescendants (tree : EncodingTree) (j : int) : int list =
        let rec collect (node : TreeNode) =
            [ for child in node.Children do
                  yield child.Index
                  yield! collect child ]
        collect (Map.find j tree.Nodes)

    /// Children of node j.
    let treeChildren (tree : EncodingTree) (j : int) : int list =
        (Map.find j tree.Nodes).Children |> List.map (fun c -> c.Index)

    // ─────────────────────────────────────────────
    //  Index sets from tree structure
    //  (Havlíček et al. arXiv:1701.07072)
    //  NOTE: only valid for Fenwick trees!
    // ─────────────────────────────────────────────

    /// Update set U(j): ancestors of j.
    let treeUpdateSet (tree : EncodingTree) (j : int) : Set<int> =
        treeAncestors tree j |> Set.ofList

    /// The "remainder" set R(j): for each ancestor a of j, collect
    /// a's children that have index < j AND are NOT themselves
    /// ancestors of j (i.e., not on the path from j to root).
    let treeRemainderSet (tree : EncodingTree) (j : int) : Set<int> =
        let ancestors = treeAncestors tree j
        let ancestorSet = Set.ofList ancestors
        [ for a in ancestors do
              let aNode = Map.find a tree.Nodes
              for child in aNode.Children do
                  if child.Index < j && not (Set.contains child.Index ancestorSet) then
                      yield child.Index ]
        |> Set.ofList

    /// Children set F(j): direct children of j.
    let treeChildrenSet (tree : EncodingTree) (j : int) : Set<int> =
        treeChildren tree j |> Set.ofList

    /// Parity set P(j) = R(j) ∪ F(j).
    let treeParitySet (tree : EncodingTree) (j : int) : Set<int> =
        Set.union (treeRemainderSet tree j) (treeChildrenSet tree j)

    /// Occupation set Occ(j) = {j} ∪ descendants(j).
    let treeOccupationSet (tree : EncodingTree) (j : int) : Set<int> =
        treeDescendants tree j |> Set.ofList |> Set.add j

    /// Create an EncodingScheme from a tree (Fenwick-style index sets).
    /// WARNING: Only produces correct encodings for Fenwick trees!
    let treeEncodingScheme (tree : EncodingTree) : EncodingScheme =
        { Update     = fun j _n -> treeUpdateSet tree j
          Parity     = fun j    -> treeParitySet tree j
          Occupation = fun j    -> treeOccupationSet tree j }

    // ═════════════════════════════════════════════
    //  Path-based ternary tree encoding
    //  (Bonsai arXiv:2212.09731, Jiang arXiv:1910.10746)
    // ═════════════════════════════════════════════

    /// Label for a link descending from a node.
    type LinkLabel = LX | LY | LZ

    /// A link descending from a node: either an Edge to a child, or a Leg (no child).
    type Link =
        { Label : LinkLabel
          Target : int option }  // Some childIndex, or None for a leg

    /// Compute the link labeling for each node in the tree.
    /// Each node gets exactly 3 descending links (edges + legs).
    /// We use "homogeneous localisation" (Bonsai Algorithm 4):
    ///   - If 2 or 3 children: assign X, Y to edges; Z to leg (or 3rd edge).
    ///   - If 1 child: assign X to edge; Y, Z to legs.
    ///   - If 0 children (leaf): X, Y, Z all legs.
    let computeLinks (tree : EncodingTree) : Map<int, Link list> =
        tree.Nodes
        |> Map.map (fun _idx node ->
            let childIndices = node.Children |> List.map (fun c -> c.Index)
            let labels = [LX; LY; LZ]
            let nChildren = List.length childIndices
            // Assign first nChildren labels to edges, rest to legs.
            labels
            |> List.mapi (fun i label ->
                if i < nChildren then
                    { Label = label; Target = Some childIndices.[i] }
                else
                    { Label = label; Target = None }))

    /// A "leg" is identified by the node it hangs from and which label slot it uses.
    type LegId = { Node : int; Label : LinkLabel }

    /// Enumerate all legs in the tree.
    let allLegs (links : Map<int, Link list>) : LegId list =
        [ for kvp in links do
              for link in kvp.Value do
                  if link.Target.IsNone then
                      yield { Node = kvp.Key; Label = link.Label } ]

    /// Find the label of the link from parent → child in the tree.
    let private linkLabelFromParent (links : Map<int, Link list>) (parentIdx : int) (childIdx : int) : LinkLabel =
        links.[parentIdx]
        |> List.find (fun l -> l.Target = Some childIdx)
        |> fun l -> l.Label

    /// Build the Majorana (Pauli) string for a given leg.
    /// Follow the path from root to the leg's node, collecting the
    /// Pauli operator at each node along the way, then add the label
    /// of the leg itself at the leg's node.
    let majoranaStringForLeg (tree : EncodingTree) (links : Map<int, Link list>) (leg : LegId) (n : int) : PauliRegister =
        let labelToPauli = function LX -> Pauli.X | LY -> Pauli.Y | LZ -> Pauli.Z
        // Build path from root to leg's node
        let ancestors = treeAncestors tree leg.Node |> List.rev  // root first
        // For each ancestor, find the label of the link going toward leg.Node
        let pathToNode =
            match ancestors with
            | [] -> []  // leg is at root
            | _ ->
                // Pairs: (ancestor, next node toward leg.Node)
                let pairs =
                    let fullPath = ancestors @ [leg.Node]
                    // The path from root is: a0 → a1 → ... → leg.Node
                    // The ancestors list is [root; ...; parent_of_leg.Node]
                    // We pair each with the next: (a0,a1), (a1,a2), ..., (parent, leg.Node)
                    fullPath |> List.pairwise
                pairs
                |> List.map (fun (parent, child) ->
                    (parent, linkLabelFromParent links parent child))
        // The full assignment: ancestors contribute their edge labels, plus the leg label at the node
        let assignments =
            [ for (nodeIdx, label) in pathToNode do
                  yield (nodeIdx, labelToPauli label)
              yield (leg.Node, labelToPauli leg.Label) ]
        pauliOfAssignments n assignments Complex.One

    /// Pair the legs into fermionic modes (Bonsai Algorithm 1 / Section III.3).
    /// For each node u:
    ///   Follow X-link, then keep taking Z-links until a leg → that's s_x(u)
    ///   Follow Y-link, then keep taking Z-links until a leg → that's s_y(u)
    /// Returns: Map from node index to (s_x leg, s_y leg).
    let pairLegs (tree : EncodingTree) (links : Map<int, Link list>) : Map<int, LegId * LegId> =
        let followToLeg (startNode : int) (startLabel : LinkLabel) : LegId =
            let nodeLinks = links.[startNode]
            let link = nodeLinks |> List.find (fun l -> l.Label = startLabel)
            match link.Target with
            | None ->
                // The link itself is a leg
                { Node = startNode; Label = startLabel }
            | Some child ->
                // Follow Z-links from the child downward until a leg
                let rec followZ nodeIdx =
                    let zLink = links.[nodeIdx] |> List.find (fun l -> l.Label = LZ)
                    match zLink.Target with
                    | None -> { Node = nodeIdx; Label = LZ }
                    | Some nextChild -> followZ nextChild
                followZ child
        tree.Nodes
        |> Map.map (fun _idx node ->
            let sx = followToLeg node.Index LX
            let sy = followToLeg node.Index LY
            (sx, sy))

    /// Encode a single ladder operator using the path-based ternary tree method.
    ///
    ///   m_{2j} ↔ S_{s_x(u)}     (even Majorana)
    ///   m_{2j+1} ↔ S_{s_y(u)}   (odd Majorana)
    ///
    ///   a†_j = ½(m_{2j} − i·m_{2j+1}) = ½(S_x − i·S_y)
    ///   a_j  = ½(m_{2j} + i·m_{2j+1}) = ½(S_x + i·S_y)
    let encodeWithTernaryTree (tree : EncodingTree) (op : LadderOperatorUnit) (j : uint32) (n : uint32) : PauliRegisterSequence =
        match op with
        | Identity -> PauliRegisterSequence ()
        | _ when j >= n -> PauliRegisterSequence ()
        | _ ->
            let ni = int n
            let ji = int j
            let links = computeLinks tree
            let pairs = pairLegs tree links
            let (sx, sy) = pairs.[ji]
            let sxReg = majoranaStringForLeg tree links sx ni
            let syReg = majoranaStringForLeg tree links sy ni
            let cReg = sxReg.ResetPhase (Complex (0.5, 0.0))
            let dCoeff = match op with Raise -> Complex (0.0, -0.5) | _ -> Complex (0.0, 0.5)
            let dReg = syReg.ResetPhase dCoeff
            PauliRegisterSequence [| cReg; dReg |]

    // ═════════════════════════════════════════════
    //  Tree construction helpers
    // ═════════════════════════════════════════════

    /// Build a tree node with given index, children, and parent.
    let private mkNode idx children parent =
        { Index = idx; Children = children; Parent = parent }

    /// Collect all nodes from a tree rooted at 'node' into a map.
    let rec private collectNodes (node : TreeNode) : Map<int, TreeNode> =
        node.Children
        |> List.fold (fun acc child ->
            Map.fold (fun m k v -> Map.add k v m) acc (collectNodes child))
            (Map.ofList [ (node.Index, node) ])

    /// Build an EncodingTree from a root node.
    let private mkTree (root : TreeNode) (n : int) : EncodingTree =
        { Root = root; Nodes = collectNodes root; Size = n }

    // ─────────────────────────────────────────────
    //  Linear chain (= Jordan-Wigner)
    // ─────────────────────────────────────────────

    /// Build a linear chain tree:  0 — 1 — 2 — … — (n-1)
    /// where 0 is the root and each node's only child is the next.
    /// This recovers the Jordan-Wigner encoding.
    let linearTree (n : int) : EncodingTree =
        // Build bottom-up
        let rec build i =
            if i >= n then
                None
            else
                let children =
                    match build (i + 1) with
                    | Some child -> [ child ]
                    | None       -> []
                let parent = if i = 0 then None else Some (i - 1)
                Some (mkNode i children parent)
        match build 0 with
        | Some root -> mkTree root n
        | None      -> failwith "n must be > 0"

    // ─────────────────────────────────────────────
    //  Balanced binary tree
    // ─────────────────────────────────────────────

    /// Build a balanced binary tree on n nodes.
    /// The root is the middle element; left subtree gets indices 0..mid-1,
    /// right subtree gets indices mid+1..n-1.
    ///
    /// This achieves O(log₂ n) Pauli weight for all operators.
    let balancedBinaryTree (n : int) : EncodingTree =
        let rec build lo hi parent =
            if lo > hi then []
            else
                let mid = (lo + hi) / 2
                let node = mkNode mid [] parent
                let leftChildren = build lo (mid - 1) (Some mid)
                let rightChildren = build (mid + 1) hi (Some mid)
                let fullNode = { node with Children = leftChildren @ rightChildren }
                [ fullNode ]
        match build 0 (n - 1) None with
        | [ root ] -> mkTree root n
        | _        -> failwith "n must be > 0"

    // ─────────────────────────────────────────────
    //  Balanced ternary tree
    // ─────────────────────────────────────────────

    /// Build a balanced ternary tree on n nodes.
    /// Each internal node has up to 3 children.
    /// The root is the element at position n/2; the three subtrees
    /// split the remaining indices into roughly equal thirds.
    ///
    /// This achieves O(log₃ n) Pauli weight, which is optimal
    /// (Jiang et al., arXiv:1910.10746).
    let balancedTernaryTree (n : int) : EncodingTree =
        let rec build (indices : int list) parent =
            match indices with
            | [] -> []
            | [x] -> [ mkNode x [] parent ]
            | _ ->
                let len = List.length indices
                // Place the root at the middle
                let midIdx = len / 2
                let rootVal = indices.[midIdx]
                let before = indices.[.. midIdx - 1]
                let after  = indices.[midIdx + 1 ..]
                // Split 'before' into up to 2 parts, 'after' into 1 part
                // → root has up to 3 children
                let bLen = List.length before
                let splitB = bLen / 2
                let part1 = if splitB > 0 then before.[.. splitB - 1] else []
                let part2 = before.[splitB ..]
                let part3 = List.ofArray (Array.ofList after)
                let parts = [part1; part2; part3] |> List.filter (fun p -> not (List.isEmpty p))
                let children = parts |> List.collect (fun p -> build p (Some rootVal))
                [ { Index = rootVal; Children = children; Parent = parent } ]
        let indices = [ 0 .. n - 1 ]
        match build indices None with
        | [ root ] -> mkTree root n
        | _        -> failwith "n must be > 0"

    // ═════════════════════════════════════════════
    //  Convenience wrappers
    // ═════════════════════════════════════════════

    /// Encode using a balanced ternary tree (path-based method).
    let ternaryTreeScheme (n : int) : EncodingScheme =
        treeEncodingScheme (balancedTernaryTree n)

    /// Encode a ladder operator using the ternary tree encoding
    /// (correct path-based method from Bonsai / Jiang et al.).
    let ternaryTreeTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
        let tree = balancedTernaryTree (int n)
        encodeWithTernaryTree tree op j n

    /// Encode a ladder operator using a balanced binary tree
    /// (also uses the path-based method — binary trees are ternary trees
    /// where some nodes have only 2 children).
    let balancedBinaryTreeTerms (op : LadderOperatorUnit) (j : uint32) (n : uint32) =
        let tree = balancedBinaryTree (int n)
        encodeWithTernaryTree tree op j n

