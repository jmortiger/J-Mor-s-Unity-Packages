using System;
using System.Linq.Expressions;

namespace JMor.Utility
{
	public static class Util
	{
		public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess) => ((MemberExpression)memberAccess.Body).Member.Name;
		public static string GetClassMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess) => $"{typeof(T).Name}.{((MemberExpression)memberAccess.Body).Member.Name}";
		public static string GetClassMemberNameFull<T, TValue>(Expression<Func<T, TValue>> memberAccess) => $"{typeof(T).FullName}.{((MemberExpression)memberAccess.Body).Member.Name}";
		public static string GetClassMemberNameAssembly<T, TValue>(Expression<Func<T, TValue>> memberAccess) => $"[{typeof(T).AssemblyQualifiedName}].{((MemberExpression)memberAccess.Body).Member.Name}";
	}
}