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

        protected readonly CMidiEventSettings m_settings;

        protected MethodInfo m_methodInfo;
        protected Action m_action; // Derived types hide this with their own implementations

        public CMidiAction(object target, string methodName, CMidiEventSettings settings)
        {
            m_target = target;
            m_targetType = m_target.GetType();
            m_methodName = methodName;
            m_settings = settings;

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
    class CMidiActionBool : CMidiAction
    {
        protected new Action<bool> m_action;

        public CMidiActionBool(object target, string methodName, CMidiEventSettings settings)
            : base(target, methodName, settings)
        {
        }

        protected override void Init()
        {
            m_methodInfo = m_targetType.GetMethod(m_methodName, new Type[] { typeof(bool) });
            m_action = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), m_target, m_methodInfo);
        }

        public override void Trigger()
        {
            m_action(m_settings.BoolOption1);
        }
    }

    // For methods with arguments of type HashSet<CPropertyBase> and bool
    class CMidiActionSelectionBool : CMidiActionBool
    {
        protected new Action<HashSet<CPropertyBase>, bool> m_action;
        
        public CMidiActionSelectionBool(object target, string methodName, CMidiEventSettings settings)
            : base(target, methodName, settings)
        {
        }

        protected override void Init()
        {
            m_methodInfo = m_targetType.GetMethod(m_methodName, new Type[] { typeof(HashSet<CPropertyBase>), typeof(bool) });
            m_action = (Action<HashSet<CPropertyBase>, bool>)Delegate.CreateDelegate(typeof(Action<HashSet<CPropertyBase>, bool>), m_target, m_methodInfo);
        }

        public override void Trigger()
        {
            m_action(m_settings.Selection.SelectedProperties, m_settings.BoolOption1);
        }
    }
}
