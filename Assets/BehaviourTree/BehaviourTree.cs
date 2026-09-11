using System;
using System.Collections.Generic;

/// <summary>
/// ==========================================================================
///  BEHAVIOUR TREE CORE FRAMEWORK
/// ==========================================================================
/// This file contains the generic, reusable "building blocks" of a behaviour
/// tree. It knows NOTHING about your specific enemy, the axe, the player,
/// or the timer — it's just the plumbing.
///
/// The enemy-specific logic (chasing, waiting on the bed, attacking, etc.)
/// lives in EnemyAI.cs, which USES these classes to build its own tree.
///
/// WHY A BEHAVIOUR TREE?
/// Instead of one giant Update() function full of if/else statements,
/// a behaviour tree lets you build AI logic out of small, reusable pieces
/// ("nodes") that get evaluated every frame from the root down. This makes
/// it much easier to add new behaviours later without breaking old ones.
/// ==========================================================================
/// </summary>

namespace EnemyAI_BT
{
    /// <summary>
    /// Every node in a behaviour tree returns one of these three states
    /// every time it is evaluated (checked) on a given frame:
    ///
    /// - Success: the node finished doing its job successfully.
    /// - Failure: the node could not do its job / condition was false.
    /// - Running: the node is still in progress (e.g. still walking to a point)
    ///            and needs to be evaluated again next frame.
    /// </summary>
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }

    /// <summary>
    /// The base class that ALL behaviour tree nodes inherit from.
    /// A "node" is just anything with an Evaluate() method that
    /// returns a NodeState.
    /// </summary>
    public abstract class BTNode
    {
        public abstract NodeState Evaluate();
    }

    /// <summary>
    /// SELECTOR NODE ("try each child until one works")
    /// -------------------------------------------------
    /// Think of this like an "OR" in the tree.
    /// It goes through its children IN ORDER, and:
    ///   - if a child returns Success -> the Selector immediately returns Success
    ///   - if a child returns Running -> the Selector returns Running (still busy)
    ///   - if a child returns Failure -> the Selector moves on to try the NEXT child
    /// If ALL children fail, the Selector itself returns Failure.
    ///
    /// This is perfect for "priority lists" — e.g. "try to attack, otherwise
    /// try to chase, otherwise just idle" — because it always evaluates the
    /// highest-priority behaviour first.
    /// </summary>
    public class Selector : BTNode
    {
        private readonly List<BTNode> children;

        public Selector(List<BTNode> children)
        {
            this.children = children;
        }

        public override NodeState Evaluate()
        {
            foreach (BTNode child in children)
            {
                NodeState result = child.Evaluate();

                switch (result)
                {
                    case NodeState.Success:
                        return NodeState.Success; // found a working behaviour, stop here
                    case NodeState.Running:
                        return NodeState.Running; // still busy with this behaviour
                    case NodeState.Failure:
                        continue; // this one didn't work, try the next child
                }
            }

            // Nothing in the list worked this frame.
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// SEQUENCE NODE ("do each child in order, all must succeed")
    /// -------------------------------------------------------------
    /// Think of this like an "AND" in the tree.
    /// It goes through its children IN ORDER, and:
    ///   - if a child returns Failure -> the Sequence immediately returns Failure
    ///   - if a child returns Running -> the Sequence returns Running (still busy)
    ///   - if a child returns Success -> the Sequence moves on to the NEXT child
    /// If ALL children succeed, the Sequence itself returns Success.
    ///
    /// This is perfect for "condition + action" pairs — e.g.
    /// "IF player has axe AND is in range -> THEN attack".
    /// </summary>
    public class Sequence : BTNode
    {
        private readonly List<BTNode> children;

        public Sequence(List<BTNode> children)
        {
            this.children = children;
        }

        public override NodeState Evaluate()
        {
            foreach (BTNode child in children)
            {
                NodeState result = child.Evaluate();

                switch (result)
                {
                    case NodeState.Failure:
                        return NodeState.Failure; // a required step failed, whole sequence fails
                    case NodeState.Running:
                        return NodeState.Running; // still working through this step
                    case NodeState.Success:
                        continue; // this step is done, move to the next one
                }
            }

            // Every single step succeeded.
            return NodeState.Success;
        }
    }

    /// <summary>
    /// CONDITION NODE (a "leaf" that only checks something, no side effects)
    /// -----------------------------------------------------------------------
    /// Wraps a simple bool-returning function (e.g. "PlayerHasAxe()") and
    /// converts it into a behaviour tree node:
    ///   true  -> Success
    ///   false -> Failure
    /// Conditions never return Running — they're instant checks.
    /// </summary>
    public class ConditionNode : BTNode
    {
        private readonly Func<bool> condition;

        public ConditionNode(Func<bool> condition)
        {
            this.condition = condition;
        }

        public override NodeState Evaluate()
        {
            return condition() ? NodeState.Success : NodeState.Failure;
        }
    }

    /// <summary>
    /// ACTION NODE (a "leaf" that actually DOES something)
    /// -------------------------------------------------------
    /// Wraps a function that performs behaviour (moving, attacking, idling)
    /// and directly returns whatever NodeState that function decides is
    /// appropriate (e.g. Running while still walking, Success once arrived).
    /// </summary>
    public class ActionNode : BTNode
    {
        private readonly Func<NodeState> action;

        public ActionNode(Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Evaluate()
        {
            return action();
        }
    }
}
