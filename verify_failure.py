import networkx as nx


def get_ancestors(G, node):
    ancestors = set()
    curr = list(G.predecessors(node))
    while curr:
        p = curr[0]
        ancestors.add(p)
        curr = list(G.predecessors(p))
    return ancestors


def get_descendants(G, node):
    return nx.descendants(G, node)


def get_children(G, node):
    return set(G.successors(node))


def get_R(G, j):
    # R(j) = {k : child of ancestor of j, k < j, k not in U(j)}
    ancestors = get_ancestors(G, j)
    R = set()
    for a in ancestors:
        children_a = get_children(G, a)
        for k in children_a:
            if k < j and k not in ancestors and k != j:  # k not in U(j)
                R.add(k)
    return R


def get_P(G, j):
    return get_children(G, j).union(get_R(G, j))


def get_Occ(G, j):
    return get_descendants(G, j).union({j})


def get_c_ops(G, j):
    U_j = get_ancestors(G, j)
    P_j = get_P(G, j)

    # c_j = X_{U(j) u {j}} Z_{P(j)}
    ops = {}

    # X terms
    for k in U_j:
        ops[k] = ops.get(k, "I")
        if ops[k] == "I":
            ops[k] = "X"
        elif ops[k] == "Z":
            ops[k] = "Y"  # Z*X = iY (ignoring phase)
        elif ops[k] == "X":
            ops[k] = "I"

    k = j
    ops[k] = ops.get(k, "I")
    if ops[k] == "I":
        ops[k] = "X"
    elif ops[k] == "Z":
        ops[k] = "Y"
    elif ops[k] == "X":
        ops[k] = "I"

    # Z terms
    for k in P_j:
        current = ops.get(k, "I")
        if current == "I":
            ops[k] = "Z"
        elif current == "X":
            ops[k] = "Y"  # X*Z = -iY
        elif current == "Z":
            ops[k] = "I"

    return ops


def get_d_ops(G, j):
    U_j = get_ancestors(G, j)
    P_j = get_P(G, j)
    Occ_j = get_Occ(G, j)

    # Z set: (P(j) XOR Occ(j)) \ {j}
    Z_set = (P_j.symmetric_difference(Occ_j)) - {j}

    ops = {}

    # Y_j
    ops[j] = "Y"

    # X terms from U(j)
    for k in U_j:
        current = ops.get(k, "I")
        if current == "I":
            ops[k] = "X"
        elif current == "Y":
            ops[k] = "Z"  # Y*X = -iZ

    # Z terms
    for k in Z_set:
        current = ops.get(k, "I")
        if current == "I":
            ops[k] = "Z"
        elif current == "X":
            ops[k] = "Y"  # X*Z = -iY
        elif current == "Y":
            ops[k] = "X"  # Y*Z = iX

    return ops


def check_anticommute(ops1, ops2, nodes):
    anticommutes = 0
    for k in nodes:
        op1 = ops1.get(k, "I")
        op2 = ops2.get(k, "I")

        if op1 != "I" and op2 != "I" and op1 != op2:
            anticommutes += 1

    # If odd number of anticommuting factors, they anticommute overall.
    # If even, they commute.
    return anticommutes % 2 != 0, anticommutes


def run_test(w, u, v):
    G = nx.DiGraph()
    G.add_edge(w, u)
    G.add_edge(u, v)

    nodes = [w, u, v]

    # Calculate d_w
    d_w = get_d_ops(G, w)

    # Calculate c_v
    c_v = get_c_ops(G, v)

    is_anticommuting, count = check_anticommute(d_w, c_v, nodes)

    print(f"Path {w}->{u}->{v}")
    print("Properties:")
    print(f"  w={w}: U={get_ancestors(G, w)}, P={get_P(G, w)}, Occ={get_Occ(G, w)}")
    print(f"  u={u}: U={get_ancestors(G, u)}, P={get_P(G, u)}, Occ={get_Occ(G, u)}")
    print(f"  v={v}: U={get_ancestors(G, v)}, P={get_P(G, v)}, Occ={get_Occ(G, v)}")
    print("Operators:")
    print(f"  d_{w}: {d_w}")
    print(f"  c_{v}: {c_v}")
    print("Anticommutation check:")
    print(f"  Anticommuting factors: {count}")
    print(
        f"  Result: {'Anticommutes (Success)' if is_anticommuting else 'Commutes (FAILURE)'}"
    )
    print("-" * 20)


print("Verifying Construction A failure mechanism...")
# Case 1: Increasing Indices (Standard)
run_test(0, 1, 2)

# Case 2: Mixed Indices (just to see if it matters, though less likely for traversal)
run_test(2, 0, 1)  # w=2, u=0, v=1 (2->0->1)
