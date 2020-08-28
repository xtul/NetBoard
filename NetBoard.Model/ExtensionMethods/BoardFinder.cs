using NetBoard.Model.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace NetBoard.Model.ExtensionMethods {
	public static class BoardFinder {
		public static IEnumerable<Type> GetBoards() {
			var assembly = Assembly.GetExecutingAssembly().GetName().Name;
			return Assembly.Load(assembly).GetTypes().Where(x => x.BaseType == typeof(PostStructure));
		}

		public static List<string> GetBoardsAsStrings() {
			var typeList = GetBoards();
			var tempList = new List<string>();
			foreach (var type in typeList) {
				tempList.Add(type.Name.ToLower());
				tempList.Add(type.FullName.ToLower());
			}
			return tempList;
		}

		public static List<string> GetBoardsAsFullNameStrings() {
			var typeList = GetBoards();
			var tempList = new List<string>();
			foreach (var type in typeList) {
				tempList.Add(type.FullName);
			}
			return tempList;
		}

		public static Dictionary<string, string> GetBoardsAsDict() {
			var typeList = GetBoards();
			var tempList = new Dictionary<string, string>();
			foreach (var type in typeList) {
				tempList.Add(type.Name.ToLower(), (string)type.GetField("BoardName").GetRawConstantValue());
			}
			return tempList;
		}

		public static string GetBoardsAsJson() {
			var typeList = GetBoards();
			var tempDict = new Dictionary<string, string>();
			foreach (var type in typeList) {
				tempDict.Add(type.Name.ToLower(), $"/{type.Name.ToLower()}/");
			}
			return JsonConvert.SerializeObject(tempDict);
		}

		public static Type GetBoard(string board) {
			var assembly = Assembly.GetExecutingAssembly().GetName().Name;
			return Assembly.Load(assembly).GetTypes().Where(x => x.BaseType == typeof(PostStructure)).First(x => x.Name.ToUpper() == board.ToUpper());
		}

		public static bool BoardExists(string board) {
			return GetBoard(board) != null;
		}

		public static string GetSchemaAndTable<T>(this DbContext context) where T : class {
			var entityType = context.Model.FindEntityType(typeof(T));
			return $"{entityType.GetSchema()}.{entityType.GetTableName()}";
		}
	}
}
