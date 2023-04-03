using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

// CMidiAction classes to bind MIDI event to methods of an object
namespace Snapshot
{
    // Base class for triggering methods with zero arguments
    public class CMidiAction
    {
        protected readonly object m_target;
        protected readonly string m_methodName;
        protected readonly Type m_targetType;

        protected MethodInfo m_methodInfo;
        protected Action m_action; // Derived types hide this with their own implementations

        public CMidiAction(object target, string methodName)
        {
            m_target = target;
            m_targetType = m_target.GetType();
            m_methodName = methodName;

            Init();
        }

        protected virtual void Init()
        {
            m_methodInfo = m_targetType.GetMethod(m_methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
            m_action = (Action)Delegate.CreateDelegate(typeof(Action), m_target, m_methodInfo);
        }

        public virtual void Trigger()
        {
            m_action();
        }

        public override int GetHashCode()
        {
            return m_methodInfo.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            CMidiAction that = obj as CMidiAction;
            bool sameObject = ReferenceEquals(this.m_target, that.m_target);
            bool sameMethod = m_methodInfo.Name == that.m_methodInfo.Name;
            return sameObject && sameMethod;
        }
    };

    // For methods with a single argument of type HashSet<CPropertyBase>
    class CMidiActionSelection : CMidiAction
    {
        protected HashSet<CPropertyBase> m_selection;
        protected new Action<HashSet<CPropertyBase>> m_action;

        public CMidiActionSelection(object target, string methodName, HashSet<CPropertyBase> selection)
            : base(target, methodName)
        {
            m_selection = new HashSet<CPropertyBase>(selection);
        }

        protected override void Init()
        {
            m_methodInfo = m_targetType.GetMethod(m_methodName, new Type[] { typeof(HashSet<CPropertyBase>) });
            m_action = (Action<HashSet<CPropertyBase>>)Delegate.CreateDelegate(typeof(Action<HashSet<CPropertyBase>>), m_target, m_methodInfo);
        }

        public override void Trigger()
        {
            m_action(m_selection);
        }
    }

    // For methods with arguments of type HashSet<CPropertyBase> and bool
    class CMidiActionSelectionBool : CMidiActionSelection
    {
        protected new Action<HashSet<CPropertyBase>, bool> m_action;
        protected bool m_bval;

        public CMidiActionSelectionBool(object target, string methodName, HashSet<CPropertyBase> selection, bool bval)
            : base(target, methodName, selection)
        {
            m_bval = bval;
        }

        protected override void Init()
        {
            m_methodInfo = m_targetType.GetMethod(m_methodName, new Type[] { typeof(HashSet<CPropertyBase>), typeof(bool) });
            m_action = (Action<HashSet<CPropertyBase>, bool>)Delegate.CreateDelegate(typeof(Action<HashSet<CPropertyBase>, bool>), m_target, m_methodInfo);
        }

        public override void Trigger()
        {
            m_action(m_selection, m_bval);
        }
    }
}
