    using System.Collections.Generic;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Orchard.Mvc.Routes;

namespace River.SecureFileStorage
{
    public class Routes : IRouteProvider
    {
        public void GetRoutes( ICollection<RouteDescriptor> routes )
        {
            foreach ( var routeDescriptor in this.GetRoutes( ) )
                routes.Add( routeDescriptor );
        }

        public IEnumerable<RouteDescriptor> GetRoutes( )
        {
            return new[ ] {
                new RouteDescriptor {
                    Priority = 100,
                    Route = new Route(
                        "SecureFile/{id}",
                        new RouteValueDictionary {
                            { "area", "River.SecureFileStorage" },
                            { "controller", "SecureFile" },
                            { "action", "Index" }
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            { "area", "River.SecureFileStorage" }
                        },
                        new MvcRouteHandler())
                }
            };
        }
    }
}