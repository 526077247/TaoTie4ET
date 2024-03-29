﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TaoTie
{
    public class AssemblyManager:IManager
    {
        public static AssemblyManager Instance => ManagerProvider.RegisterManager<AssemblyManager>();
        private HashSet<Assembly> Temp;
        private HashSet<Assembly> HotfixTemp;
        private Dictionary<string, Type> allTypes;
        private UnOrderMultiMap<Assembly, Type> mapTypes;
        #region override

        public void Init()
        {
            Temp = new HashSet<Assembly>();
            HotfixTemp = new HashSet<Assembly>();
            allTypes = new Dictionary<string, Type>();
            mapTypes = new UnOrderMultiMap<Assembly, Type>();
        }

        public void Destroy()
        {
            Temp.Clear();
        }

        #endregion

        public Dictionary<string, Type> GetTypes()
        {
            return allTypes;
        }

        public void AddAssembly(Assembly assembly)
        {
            if (!Temp.Contains(assembly))
            {
                foreach (Type type in assembly.GetTypes())
                {
                    allTypes[type.FullName] = type;
                    mapTypes.Add(assembly,type);
                }
            }
        }
        
        public void AddHotfixAssembly(Assembly assembly)
        {
            HotfixTemp.Add(assembly);
            AddAssembly(assembly);
        }

        public void RemoveHotfixAssembly()
        {
            foreach (var assembly in HotfixTemp)
            {
                if (mapTypes.TryGetValue(assembly, out var types))
                {
                    foreach (var type in types)
                    {
                        allTypes.Remove(type.FullName);
                    }
                }

                mapTypes.Remove(assembly);
                Temp.Remove(assembly);
            }
            HotfixTemp.Clear();
        }
        
        public Type GetType(string typeName)
        {
            if (this.allTypes.TryGetValue(typeName, out var res))
            {
                return res;
            }
            Log.Error(typeName+" not found");
            return null;
        }
    }
}