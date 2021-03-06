﻿#region License
/*
The MIT License

Copyright (c) 2008 Sky Morey

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
#endregion
using System.Linq;
using System.Patterns.Generic;
using System.Collections.Generic;
namespace System.Web.Routing
{
    /// <summary>
    /// DynamicRoute
    /// </summary>
    public class DynamicRoute : Route
    {
        private IDynamicRoutingContext _routingContextAsFixed;
        private Func<DynamicRoute, IDynamicRoutingContext> _routingContext;

        public DynamicRoute()
            : this(new SiteMapDynamicRoutingContext((ISiteMapProvider)SiteMap.Provider)) { }
        public DynamicRoute(ISiteMapProvider siteMapProvider)
            : this(new SiteMapDynamicRoutingContext(siteMapProvider)) { }
        public DynamicRoute(SiteMapProvider siteMapProvider)
            : this((ISiteMapProvider)siteMapProvider) { }
        public DynamicRoute(IDynamicRoutingContext routingContext)
            : base(null, null)
        {
            _routingContextAsFixed = routingContext;
            RoutingContext = (r => _routingContextAsFixed);
        }
        public DynamicRoute(Func<DynamicRoute, IDynamicRoutingContext> routingContext)
            : base(null, null)
        {
            RoutingContext = routingContext;
        }

        public static void SetRouteDefaults(IEnumerable<Route> routes, IDynamicNode node)
        {
            var id = node.Key;
            foreach (var route in routes)
            {
                route.DataTokens["dynamicNode"] = node;
                route.Defaults["dynamicId"] = id;
            }
        }

        public override RouteData GetRouteData(HttpContextBase httpContext)
        {
            if (httpContext == null)
                throw new ArgumentNullException("httpContext");
            var httpRequest = httpContext.Request;
            // virtualPath modeled from Route::GetRouteData
            string virtualPath = httpRequest.AppRelativeCurrentExecutionFilePath.Substring(1) + httpRequest.PathInfo;
            //var requestUri = _siteMapExRouteContext.GetRequestUri(httpContext);
            var node = GetNode(_routingContext(this), virtualPath);
            if (node != null)
            {
                // func
                var func = node.Get<Func<IDynamicNode, RouteData>>();
                if (func != null)
                    return func(node);
                // single
                var route = node.Get<Route>();
                if (route != null)
                    return route.GetRouteData(httpContext);
                // many
                var multiRoutes = node.GetMany<Route>();
                if (multiRoutes != null)
                    foreach (var multiRoute in multiRoutes)
                    {
                        var data = multiRoute.GetRouteData(httpContext);
                        if (data != null)
                            return data;
                    }
            }
            return null;
        }

        public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary values)
        {
            if (requestContext == null)
                throw new ArgumentNullException("requestContext");
            var node = GetNode(_routingContext(this), values, true);
            if (node != null)
            {
                // func
                var func = node.Get<Func<IDynamicNode, VirtualPathData>>();
                if (func != null)
                    return func(node);
                // single
                var route = node.Get<Route>();
                if (route != null)
                    return route.GetVirtualPath(requestContext, values);
                // many
                var multiRoutes = node.GetMany<Route>();
                if (multiRoutes != null)
                    foreach (var multiRoute in multiRoutes)
                    {
                        var path = multiRoute.GetVirtualPath(requestContext, values);
                        if (path != null)
                            return path;
                    }
            }
            return null;
        }

        public Func<DynamicRoute, IDynamicRoutingContext> RoutingContext
        {
            get { return _routingContext; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _routingContext = value;
            }
        }

        private static IDynamicNode GetNode(IDynamicRoutingContext routingContext, string path)
        {
            return routingContext.FindNode(path);
        }

        private static IDynamicNode GetNode(IDynamicRoutingContext routingContext, RouteValueDictionary values, bool removeValue)
        {
            object value;
            if (values.TryGetValue("dynamicId", out value))
            {
                if (removeValue)
                    values.Remove("dynamicId");
                return routingContext.FindNodeById(value as string);
            }
            return null;
        }
    }
}
