using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;
using Orchard.Mvc.Routes;

namespace FileManager
{
    public class Routes : IRouteProvider
    {

        public void GetRoutes(ICollection<RouteDescriptor> routes)
        {
            foreach (var routeDescriptor in GetRoutes())
                routes.Add(routeDescriptor);
        }

        public IEnumerable<RouteDescriptor> GetRoutes()
        {
            return new[] {
                 new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Admin/Media",
                        new RouteValueDictionary {
                            {"area", "FileManager"},
                            {"controller", "Admin"},
                            {"action", "Index"},
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "FileManager"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Admin/Settings/Media",
                        new RouteValueDictionary {
                            {"area", "FileManager"},
                            {"controller", "Admin"},
                            {"action", "Settings"},
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "FileManager"}
                        },
                        new MvcRouteHandler())
                },
                new RouteDescriptor {
                    Priority = 5,
                    Route = new Route(
                        "Admin/MediaPicker",
                        new RouteValueDictionary {
                            {"area", "FileManager"},
                            {"controller", "Admin"},
                            {"action", "MediaPicker"},
                        },
                        new RouteValueDictionary(),
                        new RouteValueDictionary {
                            {"area", "FileManager"}
                        },
                        new MvcRouteHandler())
                }
            };
            
        }
    }
}