using NetBoard.Controllers.Generic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace NetBoard.ExtensionMethods {
	public static class BoardControllerFinder {
		public static IEnumerable<Type> GetBoardControllers() {
			var assembly = Assembly.GetExecutingAssembly().GetName().Name;
			return Assembly.Load(assembly).GetTypes().Where(x => x.InheritsFrom(typeof(BoardController)));
		}

		public static List<string> GetBoardsAsFullNameStrings() {
			var typeList = GetBoardControllers();
			var tempList = new List<string>();
			foreach (var type in typeList) {
				tempList.Add(type.FullName);
			}
			return tempList;
		}

		public static Type GetBoard(string board) {
			var assembly = Assembly.GetExecutingAssembly().GetName().Name;
			return Assembly.Load(assembly).GetTypes().Where(x => x.InheritsFrom(typeof(BoardController))).First(x => x.Name.ToUpper() == board.ToUpper());
		}

		public static bool InheritsFrom(this Type t1, Type t2) {
			if (null == t1 || null == t2)
				return false;

			if (null != t1.BaseType &&
				t1.BaseType.IsGenericType &&
				t1.BaseType.GetGenericTypeDefinition() == t2) {
				return true;
			}

			if (InheritsFrom(t1.BaseType, t2))
				return true;

			return
				(t2.IsAssignableFrom(t1) && t1 != t2)
				||
				t1.GetInterfaces().Any(x =>
				  x.IsGenericType &&
				  x.GetGenericTypeDefinition() == t2);
		}
	}
}
