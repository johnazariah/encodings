```prompt
# Pick Next Work

You are helping the user decide what to work on next. Analyze the project state and make a prioritized recommendation.

**IMPORTANT**: This prompt is for DISCUSSION and PLANNING only. Do NOT start implementing anything until the user explicitly says to proceed (e.g., "let's do #1", "start on X", "go ahead").

## Step 1: Gather Context

Read the following to understand current state:

1. **Plans** - Read `.project/plans/`:
   - Understand current cleanup and improvement plans
   - Note which tasks are completed vs. pending
   - Pay attention to dependencies between tasks

2. **Research Status** - Read `.project/research/README.md`:
   - Understand which papers are in progress
   - Check completion status for each paper
   - Note any blocking items

3. **Recent Git History** - Check momentum and recent work:
   ```bash
   git log --oneline -20
   ```

4. **Check for tagged TODOs** in source code:
   ```bash
   grep -rn "TODO\|FIXME\|XXX\|HACK" src/ test/ --include="*.fs" --include="*.fsx" | head -20
   ```

5. **Project Health** - Run checks:
   ```bash
   dotnet build 2>&1 | tail -10
   dotnet test 2>&1 | tail -10
   ```

## Step 2: Categorize Work Items

Group findings into:

| Category | Priority | Examples |
|----------|----------|----------|
| **Blocking** | 🔴 High | Failing tests, build errors, broken exports |
| **Momentum** | 🟡 Medium | Continue partially-done plans, quick wins |
| **Strategic** | 🟢 Normal | New features, paper preparation, documentation |
| **Debt** | ⚪ Low | Refactoring, TODOs, cleanup |

## Step 3: Evaluate Open Tasks

For each plan in `.project/plans/`, assess:

1. **Implementation Status** - Fully done, partial, or not started?
2. **Dependencies** - Does it require other tasks first?
3. **Effort** - Small (hours), Medium (day), Large (days)?
4. **Impact** - How critical is it to publication readiness?

## Step 4: Make Recommendation

Suggest the **top 3** work items with reasoning:

```markdown
## Recommended Next Work

### 1. [Feature/Item Name]
- **Why**: Brief justification
- **Plan**: Link to relevant plan or paper
- **Effort**: Small/Medium/Large
- **First Step**: Concrete action to start

### 2. [Alternative Option]
- **Why**: ...
- **Effort**: ...

### 3. [Another Option]
- **Why**: ...
- **Effort**: ...

## Also Consider
- Quick wins that can be done in parallel
- Debt items worth addressing opportunistically
```

## Step 5: Check Prerequisites

Before starting the recommended work:

1. Is the plan clear and complete?
2. Are there dependencies to address first?
3. Is the scope well-defined?
4. Are there existing tests that define expected behavior?

## Output Format

Present findings as:

1. **Current State Summary** - One paragraph on project health and momentum
2. **Work Items Found** - Categorized list with priorities
3. **Recommendation** - Top 3 prioritized items with reasoning
4. **Ready to Start?** - Confirmation or prerequisites needed

## STOP HERE

After presenting your recommendation, **STOP and wait for user input**.

Do NOT:
- Create files
- Write code
- Start implementation
- Run commands (except for gathering context in Steps 1-3)

DO:
- Present your analysis clearly
- Answer questions about the options
- Discuss trade-offs if asked
- Refine recommendations based on feedback

Only proceed with implementation when the user explicitly instructs you to start (e.g., "let's do #1", "start working on X", "go ahead with the recommendation").
```
