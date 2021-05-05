/* This source code licensed under the GNU Affero General Public License */

using Highpoint.Sage.ItemBased.Ports;
using Highpoint.Sage.SimCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Highpoint.Sage.ItemBased.Connectors
{

    public class ConnectorFactory
    {

        private static string _prefix = "Connector_";
        private static Dictionary<Guid, ConnectorFactory> _connectorFactories = new Dictionary<Guid, ConnectorFactory>();

        // private IModel m_model;
        private int _nextConnectorNumber = -1;

        private static ConnectorFactory ForModel(IModel model)
        {
            Guid key = GuidForModel(model);
            if (!_connectorFactories.ContainsKey(key))
            {
                _connectorFactories.Add(key, new ConnectorFactory(model));
            }
            return _connectorFactories[key];
        }

        private static Guid GuidForModel(IModel model)
        {
            return (model == null ? Guid.Empty : model.Guid);
        }

        private ConnectorFactory(IModel model)
        {
            if (model != null)
            {
                UpdateNextConnectorNumber(model, null);
            }
        }

        private void UpdateNextConnectorNumber(IModel model, string name = null)
        {
            List<string> names = new List<string>();
            if (!string.IsNullOrEmpty(name))
                names.Add(name);
            foreach (IModelObject imo in model.ModelObjects.Values)
            {
                IConnector conn = imo as IConnector;
                if (conn != null && conn.Name != null && conn.Name.StartsWith(_prefix, StringComparison.Ordinal))
                {
                    names.Add(conn.Name);
                }
            }

            foreach (string nm in names)
            {
                int connNum;
                if (int.TryParse(nm.Substring(_prefix.Length), out connNum))
                {
                    _nextConnectorNumber = Math.Max(_nextConnectorNumber, connNum);
                }
            }
        }

        public static IConnector Connect(IPort p1, IPort p2)
        {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, null);
        }

        public static IConnector Connect(IPort p1, IPort p2, string name)
        {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, name);
        }

        public static IConnector Connect(IPort p1, IPort p2, ConnectorType connType)
        {
            return Connect(p1, p2, ConnectorType.BasicNonBuffered, null);
        }

        public static IConnector Connect(IPort p1, IPort p2, ConnectorType connType, string name)
        {
            if (p1 == null || p2 == null)
            {
                throw new ApplicationException("Attempt to connect " + GetName(p1) + " to " + GetName(p2));
            }

            Debug.Assert(p1.Model == p2.Model);

            return ForModel(p1.Model)._Connect(p1, p2, connType, name);
        }

        private IConnector _Connect(IPort p1, IPort p2, ConnectorType connType, string name)
        {

            if (p1.Model != p2.Model)
            {
                string msg = string.Format("Trying to connect port {0} on object {1} to port {2} on object {3}, but they appear to be in different models.",
                    p1.Name, p1.Owner, p2.Name, p2.Owner);
                throw new ApplicationException(msg);
            }

            if (name != null)
            {

            }

            if (string.IsNullOrEmpty(name))
            {
                name = _prefix + (++_nextConnectorNumber);
            }
            else
            {
                ForModel(p1.Model).UpdateNextConnectorNumber(p1.Model, name);
            }

            if (connType.Equals(ConnectorType.BasicNonBuffered))
            {
                return new BasicNonBufferedConnector(p1.Model, name, null, Guid.NewGuid(), p1, p2);
            }
            else
            {
                throw new ApplicationException("Unknown connector type requested.");
            }
        }

        private static string GetName(IPort port)
        {
            if (port == null)
                return "<null>";
            if (port.Owner == null)
                return port + ", with a <null> owner";
            if (port.Owner is IHasIdentity)
                return port + ", owned by " + ((IHasIdentity)port.Owner).Name;
            return port + ", owned by " + port.Owner;
        }
    }
}