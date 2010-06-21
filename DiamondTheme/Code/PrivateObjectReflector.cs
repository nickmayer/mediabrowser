using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Security.Permissions;

namespace Diamond
{
    public class PrivateObjectReflector
    {
        private const BindingFlags bindingFlags
            = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;


        private Type type;    // type of class to manage
        private object instance;            // managed instance of type
        private ReflectionPermission perm;  // for non-public members
        #region .
        /// <summary>    
        /// Initializes a new instance wrapping a new instance of the    
        /// target type. One PrivateObject manages exactly one instance    
        /// of a target type.    
        /// </summary>    
        /// <param name="qualifiedTypeName">The qualified name of the type.    
        /// This should include the full assembly qualified name including    
        /// the namespace, for example "MyNamespace.MyType,MyAssemblyBaseName"    
        /// where MyNamespace is the dotted-notation namespace, MyType is the    
        /// name of the type and MyAssemblyBaseName is the base name of the    
        /// assembly containing the type.    
        /// </param>    
        /// <param name="args">An optional array of parameters to pass to    
        /// the constructor. If this argument is not specified then the    
        /// default constructor is used.  Otherwise, a constructor that    
        /// matches the number and type of parameters is used.    
        /// </param>    
        #endregion
        public PrivateObjectReflector(string qualifiedTypeName, params object[] args)
        {
            perm = new ReflectionPermission(PermissionState.Unrestricted);
            perm.Demand();
            type = Type.GetType(qualifiedTypeName);
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                types[i] = args[i].GetType();
            }
            instance = type.GetConstructor(bindingFlags, null, types, null).Invoke(args);
        }

        public object Instance
        {
            get
            {
                return instance;
            }
        }
        #region .
        /// <summary>    
        /// Gets the value of a non-public field (member variable) of the    
        /// managed type.    
        /// </summary>    
        /// <param name="name">The name of the non-public field to    
        /// interrogate</param>    
        /// <returns>A value whose type is specific to the field.</returns>    
        #endregion
        public object GetField(string name)
        {
            return type.GetField(name, bindingFlags).GetValue(instance);
        }
        #region .
        /// <summary>    
        /// Gets the value of a non-public property of the managed type.    
        /// </summary>    
        /// <param name="name">The name of the non-public property to    
        /// interrogate.</param>    
        /// <returns>A value whose type is specific to the property.</returns>    
        #endregion
        public object GetProperty(string name)
        {
            return type.GetProperty(name, bindingFlags).GetValue(instance, null);
        }
        #region .
        /// <summary>    
        /// Invokes the non-public method of the managed type.   
        /// </summary>    
        /// <param name="name">The name of the non-public method to invoke.</param>    
        /// <param name="args">And optional array of typed parameters to pass to the    
        /// method.  If this argument is not specified then the routine searches     
        /// for a method with a signature that contains not parameters.  Otherwise,    
        /// the procedure searches for a method with the number and type of parameters    
        /// specified.    
        /// </param>    
        /// <returns>A value who type is specific to the invoked method.</returns>    
        #endregion
        public object Invoke(string name, params object[] args)
        {
            Type[] types = new Type[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                types[i] = args[i].GetType();
            }
            return type.GetMethod(name, bindingFlags, null, types, null).Invoke(instance, args);
        }
        #region .
        /// <summary>   
        /// Sets the value of a non-public field (member variable) of the managed type.    
        /// </summary>    
        /// <param name="name">The name of the non-public field to modify.</param>    
        /// <param name="val">A value whose type is specific to the field.</param>    
        #endregion
        public void SetField(string name, object val)
        {
            type.GetField(name, bindingFlags).SetValue(instance, val);
        }
        #region .
        /// <summary>   
        /// Sets the value of a non-public property of the managed type.    
        /// </summary>    
        /// <param name="name">The name of the non-public property to modify.</param>    
        /// <param name="val">A value whose type is specific to the property.</param>    
        #endregion
        public void SetProperty(string name, object val)
        {
            type.GetProperty(name, bindingFlags).SetValue(instance, val, null);
        }
    }
}
