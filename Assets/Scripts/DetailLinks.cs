﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{

	public enum LinksMode {
		All,
		SelectedOnly,
		ExceptSelected
	}

	public class LinksBase {
		public bool HasConnections {
			get { return Connections.Count > 0; }
		}

		public readonly HashSet<Detail> Connections;
		public bool IsValid = true;

		public DetailLinksData Data {
			get {
				var data = new DetailLinksData {
					Connections = Connections.Select(connection => connection.GetInstanceID()).ToList(),
				};

				return data;
			}
		}

		public LinksMode LinksMode { get; private set; }

		public LinksBase(LinksMode linksMode) {
			LinksMode = linksMode;

			Connections = new HashSet<Detail>();
		}
	}

	public class DetailLinks : LinksBase
	{
		public Detail Holder { get; private set; }

		public DetailLinks( Detail detail, LinksMode linksMode = LinksMode.ExceptSelected) : base(linksMode)
		{
			Holder = detail;
		}
	}

	public class DetailsGroupLinks : LinksBase
	{
		private readonly HashSet<DetailLinks> _detailsLinks = new HashSet<DetailLinks>();

		public DetailLinks[] DetailsLinks { get { return _detailsLinks.ToArray(); } }

		public DetailsGroupLinks(LinksMode linksMode) : base(linksMode) { }

		public static DetailsGroupLinks operator +(DetailsGroupLinks groupLinks, DetailLinks detailLinks)
		{
			if (detailLinks.Holder == null) {
				Debug.LogError("Links holder can't be null in group links!");
				return groupLinks;
			}

			if (detailLinks.LinksMode != groupLinks.LinksMode) {
				Debug.LogError("Adding detail links with different links mode!");
				return groupLinks;
			}

			if (!groupLinks.IsValid) {
				return groupLinks;
			}

			if (!detailLinks.IsValid) {
				groupLinks.IsValid = false;
				return groupLinks;
			}

			groupLinks._detailsLinks.Add(detailLinks);
			groupLinks.Connections.UnionWith(detailLinks.Connections);

			return groupLinks;
		}
	}

	[Serializable]
	public class DetailLinksData {
		public List<int> Connections;

		public override string ToString() {
			var str = new StringBuilder();
			var needComma = false;

			str.Append("Direct: ");
			foreach (var connection in Connections) {
				AppController.AddComma(str, ref needComma);
				str.Append(connection);
			}

			return str.ToString();
		}
	}
}