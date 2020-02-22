using UnityEngine;
using System.Collections;
using System.Reflection;
using System;

public abstract class Singleton<T> where T : class {

	public static T instance
	{
		get
		{
			return SingletonFactory.instance;
		}
	}


	internal static class SingletonFactory
	{
		internal static T instance;

		static SingletonFactory() {
			CreateInstance(typeof(T));
		}

		public static T CreateInstance(System.Type type) {
			ConstructorInfo[] ctorPublicInfo= type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);

			if (ctorPublicInfo.Length > 0) {
				throw new Exception(
					type.FullName + " has one or more public constructors so the property cannot be enforced.");
			}
			
			ConstructorInfo ctorNonPublicInfo = type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], new ParameterModifier[0]);
			
			if (ctorNonPublicInfo == null) {
				throw new Exception(
					type.FullName + " doesn't have a private/protected constructor so the property cannot be enforced.");
			}
			
			try {
				return instance = (T)ctorNonPublicInfo.Invoke(new object[0]);
			}
			catch (Exception e) {
				throw new Exception(
					"The Singleton couldnt be constructed, check if " + type.FullName + " has a default constructor", e);
			}
		}
	}
}
