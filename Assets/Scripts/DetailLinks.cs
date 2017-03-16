using System;
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
			get { return Connections.Count > 0 || ImplicitConnections.Count > 0; }
		}

		public readonly HashSet<Detail> Connections;
		public readonly HashSet<Detail> Touches;
		public readonly HashSet<HashSet<Detail>> ImplicitConnections;

		public DetailLinksData Data {
			get {
				var data = new DetailLinksData {
					Connections = Connections.Select(connection => connection.GetInstanceID()).ToList(),
					Touches = Touches.Select(touch => touch.GetInstanceID()).ToList(),
					ImplicitConnections = new List<List<int>>()
				};

				foreach (var implicitConnection in ImplicitConnections) {
					data.ImplicitConnections.Add(
						implicitConnection.Select(connection => connection.GetInstanceID()).ToList());
				}

				return data;
			}
		}

		protected virtual DetailBase Holder { get; private set; }
		public LinksMode LinksMode { get; private set; }

		public LinksBase(LinksMode linksMode, DetailBase holder = null) {
			Holder = holder;
			LinksMode = linksMode;

			Connections = new HashSet<Detail>();
			Touches = new HashSet<Detail>();
			ImplicitConnections = new HashSet<HashSet<Detail>>();
		}

		//        public void AddConnection(Detail connection) {
		//            if (Connections.Add(connection)) {
		//                connection.Links.AddConnection(_detail);
		//            }
		//        }
		//
		//        public void RemoveConnection(Detail connection) {
		//            if (Connections.Remove(connection)) {
		//                connection.Links.RemoveConnection(_detail);
		//            }
		//        }
		//
		//        public void AddTouch(Detail touch) {
		//            if (Touches.Add(touch)) {
		//                touch.Links.AddTouch(_detail);
		//                _detail.UpdateLinks(null, true);
		//            }
		//        }
		//
		//        public void RemoveTouch(Detail touch) {
		//            if (Touches.Remove(touch)) {
		//                touch.Links.RemoveTouch(_detail);
		//                ImplicitConnections.RemoveWhere(implicitConnection => implicitConnection.Contains(touch));
		//            }
		//        }


		public bool HasWeakConnectionsWith(Detail detail, bool remove = false) {
			foreach (var implicitConnection in ImplicitConnections) {
				if (implicitConnection.Contains(detail)) {
					if (remove) {
						ImplicitConnections.RemoveWhere(connection => connection.Contains(detail));
					}

					return true;
				}
			}
			return false;
		}
	}

	public class DetailLinks : LinksBase
	{
		public new Detail Holder {
			get { return base.Holder as Detail; }
		}

		public DetailLinks(LinksMode linksMode, Detail detail = null) : base(linksMode, detail) { }
	}

	public class DetailsGroupLinks : LinksBase
	{
		private readonly Dictionary<Detail, LinksBase> _detail2Links = new Dictionary<Detail, LinksBase>();

		public new DetailsGroup Holder {
			get { return base.Holder as DetailsGroup; }
		}

		public DetailsGroupLinks(LinksMode linksMode, DetailsGroup holder = null) : base(linksMode, holder) { }

		public static DetailsGroupLinks operator +(DetailsGroupLinks groupLinks, DetailLinks detailLinks)
		{
			if (detailLinks.Holder == null)
			{
				Debug.LogError("Links holder can't be null in group links!");
				return groupLinks;
			}

			if (detailLinks.LinksMode != groupLinks.LinksMode) {
				Debug.LogError("Adding detail links with different links mode!");
				return groupLinks;
			}

			groupLinks._detail2Links.Add(detailLinks.Holder, detailLinks);

			groupLinks.Touches.UnionWith(detailLinks.Touches);
			groupLinks.Connections.UnionWith(detailLinks.Connections);
			groupLinks.ImplicitConnections.UnionWith(detailLinks.ImplicitConnections);

			return groupLinks;
		}
	}

	[Serializable]
	public class DetailLinksData {
		public List<int> Connections;
		public List<int> Touches;
		public List<List<int>> ImplicitConnections;

		public override string ToString() {
			var str = new StringBuilder();
			var needComma = false;

			str.Append("Direct: ");
			foreach (var connection in Connections) {
				AppController.AddComma(str, ref needComma);
				str.Append(connection);
			}

			str.Append(" Implicit: ");

			foreach (var @implicit in ImplicitConnections) {
				str.Append("   [ ");
				needComma = false;
				foreach (var detail in @implicit) {
					AppController.AddComma(str, ref needComma);
					str.Append(detail);
				}
				str.Append(" ]");
			}

			str.Append(" Touches: ");
			needComma = false;

			foreach (var touch in Touches) {
				AppController.AddComma(str, ref needComma);
				str.Append(touch);
			}

			return str.ToString();
		}
	}
}