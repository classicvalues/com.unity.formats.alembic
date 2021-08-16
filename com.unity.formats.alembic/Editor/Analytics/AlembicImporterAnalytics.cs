using System;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.Formats.Alembic.Sdk;

namespace UnityEditor.Formats.Alembic.Importer
{
    static class AlembicImporterAnalytics
    {
        const string VendorKey = "unity.alembic";
        const string EventName = "alembic_importer";
        const int MAXEventsPerHour = 1000;
        const int MAXNumberOfElements = 1000;

        internal static void SendAnalytics(AlembicTreeNode root, AlembicImporter importer)
        {
            if (!EditorAnalytics.enabled)
                return;

            EditorAnalytics.RegisterEventWithLimit(EventName, MAXEventsPerHour, MAXNumberOfElements, VendorKey);

            var data = CreateEvent(root, importer);
            EditorAnalytics.SendEventWithLimit(EventName, data);
        }

        static AlembicImporterAnalyticsEvent CreateEvent(AlembicTreeNode root, AlembicImporter importer)
        {
            var evt = new AlembicImporterAnalyticsEvent
            {
                material_override_count = importer.GetExternalObjectMap().Count,
                guid = AssetDatabase.AssetPathToGUID(importer.assetPath),
                app = root.stream.abcContext.GetApplication()
            };
            root.VisitRecursively(e => UpdateStats(ref evt, e));

            return evt;
        }

        static void UpdateStats(ref AlembicImporterAnalyticsEvent evt, AlembicElement e)
        {
            switch (e)
            {
                case AlembicXform:
                    evt.xform_node_count++;
                    break;
                case AlembicCamera:
                    evt.camera_node_count++;
                    break;
                case AlembicSubD m:
                    evt.sub_d_node_count++;
                    UpdateMeshStats(ref evt, m);
                    break;
                case AlembicMesh s:
                    evt.mesh_node_count++;
                    UpdateMeshStats(ref evt, s);
                    break;
                case AlembicPoints x:
                    evt.point_cloud_node_count++;
                    var points = e.abcTreeNode.gameObject.GetComponent<AlembicPointsCloud>().Positions;
                    evt.max_points_count = Math.Max(evt.max_points_count, points.Count);
                    break;
                case AlembicCurvesElement x:
                    evt.curve_node_count++;
                    var curves = e.abcTreeNode.gameObject.GetComponent<AlembicCurves>().Positions;
                    evt.max_curve_count = Math.Max(evt.max_curve_count, curves.Length);
                    break;
            }
        }

        static void UpdateMeshStats(ref AlembicImporterAnalyticsEvent evt, AlembicMesh m)
        {
            var mesh = m.abcTreeNode.gameObject.GetComponent<MeshFilter>().sharedMesh;
            var indices = 0;
            for (var i = 0; i < mesh.subMeshCount; ++i)
            {
                indices += mesh.GetSubMesh(i).indexCount;
            }

            evt.max_mesh_index_count = Math.Max(evt.max_mesh_index_count, indices);
            evt.max_mesh_vertex_count = Math.Max(evt.max_mesh_vertex_count, indices);

            evt.mesh_variable_topology |= m.summary.topologyVariance == aiTopologyVariance.Heterogeneous;
        }

        [Serializable]
        struct AlembicImporterAnalyticsEvent
        {
            public int mesh_node_count,
                       sub_d_node_count,
                       xform_node_count,
                       camera_node_count,
                       point_cloud_node_count,
                       curve_node_count,
                       max_mesh_vertex_count,
                       max_mesh_index_count,
                       max_points_count,
                       max_curve_count,
                       material_override_count;

            public string app, guid;
            public bool mesh_variable_topology;
        }
    }
}
