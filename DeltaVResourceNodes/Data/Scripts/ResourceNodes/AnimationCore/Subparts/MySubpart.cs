using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.Entity;

namespace Math0424.AnimationCore
{
    class MySubpart : IComparable
    {
        public MyEntitySubpart MyPart { get; protected set; }

        private Dictionary<Type, SubpartComponent> Components;
        private List<BaseAction> Actions;

        public Action SubpartDeleted;

        public MySubpart(MyEntitySubpart part)
        {
            this.MyPart = part;
            Actions = new List<BaseAction>();

            Components = new Dictionary<Type, SubpartComponent>();
            AddComponent<MoveComp>();
            AddComponent<EffectsComp>();

            MyPart.OnClose += InternalDelete;

            part.Name = part.EntityId.ToString();
            MyEntities.SetEntityName(part, true);
        }

        private void InternalDelete(MyEntity ent)
        {
            if (ent != null)
            {
                ent.OnClose -= InternalDelete;
            }

            Actions?.Clear();
            foreach(var c in Components?.Values)
            {
                c?.Close();
            }

            Components?.Clear();
            SubpartDeleted?.Invoke();

            MyPart = null;
            SubpartDeleted = null;
        }

        public void Delete()
        {
            InternalDelete(MyPart);
        }

        public void AddAction(BaseAction action)
        {
            Actions.Add(action);
        }

        public void InitComponents()
        {
            foreach (SubpartComponent part in Components.Values)
            {
                part.Init();
            }
        }

        public void Update()
        {
            foreach(SubpartComponent part in Components.Values)
            {
                part.Update();
            }
            for (int i = 0; i < Actions.Count; i++)
            {
                Actions[i].Update();
            }
            Actions.RemoveAll(a => a.IsFinished);
        }

        public void ClearActions()
        {
            Actions.Clear();
        }

        public int CompareTo(object obj)
        {
            if (obj is MySubpart)
            {
                if (((MySubpart)obj).MyPart?.EntityId == MyPart?.EntityId)
                {
                    return 1;
                }
                return 0;
            }
            return -1;
        }

        public override int GetHashCode()
        {
            return MyPart.EntityId.GetHashCode();
        }

        public T AddComponent<T>() where T : SubpartComponent, new()
        {
            T t = new T();
            t.Initalize(this);
            Components.Add(typeof(T), t);
            return t;
        }

        public T GetComponent<T>() where T : SubpartComponent
        {
            SubpartComponent t;
            if (Components.TryGetValue(typeof(T), out t))
            {
                return (T)t;
            }
            return null;
        }

        public bool HasComponent<T>() where T : SubpartComponent
        {
            return Components.ContainsKey(typeof(T));
        }

        //EZ components access
        public MoveComp Pos
        {
            get { return GetComponent<MoveComp>(); }
        }

        public EffectsComp Effects
        {
            get { return GetComponent<EffectsComp>(); }
        }

    }
}
