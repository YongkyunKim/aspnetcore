// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.AspNet.PipelineCore;
using Xunit;

namespace Microsoft.AspNet.Owin
{
    public class OwinEnvironmentTests
    {
        private T Get<T>(IDictionary<string, object> environment, string key)
        {
            object value;
            return environment.TryGetValue(key, out value) ? (T)value : default(T);
        }

        [Fact]
        public void OwinEnvironmentCanBeCreated()
        {
            HttpContext context = CreateContext();
            context.Request.Method = "SomeMethod";
            context.User = new ClaimsPrincipal(new ClaimsIdentity("Foo"));
            context.Request.Body = Stream.Null;
            context.Request.Headers["CustomRequestHeader"] = "CustomRequestValue";
            context.Request.Path = new PathString("/path");
            context.Request.PathBase = new PathString("/pathBase");
            context.Request.Protocol = "http/1.0";
            context.Request.QueryString = new QueryString("?key=value");
            context.Request.Scheme = "http";
            context.Response.Body = Stream.Null;
            context.Response.Headers["CustomResponseHeader"] = "CustomResponseValue";
            context.Response.StatusCode = 201;

            IDictionary<string, object> env = new OwinEnvironment(context);
            Assert.Equal("SomeMethod", Get<string>(env, "owin.RequestMethod"));
            Assert.Equal("Foo", Get<ClaimsPrincipal>(env, "server.User").Identity.AuthenticationType);
            Assert.Same(Stream.Null, Get<Stream>(env, "owin.RequestBody"));
            var requestHeaders = Get<IDictionary<string, string[]>>(env, "owin.RequestHeaders");
            Assert.NotNull(requestHeaders);
            Assert.Equal("CustomRequestValue", requestHeaders["CustomRequestHeader"].First());
            Assert.Equal("/path", Get<string>(env, "owin.RequestPath"));
            Assert.Equal("/pathBase", Get<string>(env, "owin.RequestPathBase"));
            Assert.Equal("http/1.0", Get<string>(env, "owin.RequestProtocol"));
            Assert.Equal("key=value", Get<string>(env, "owin.RequestQueryString"));
            Assert.Equal("http", Get<string>(env, "owin.RequestScheme"));

            Assert.Same(Stream.Null, Get<Stream>(env, "owin.ResponseBody"));
            var responseHeaders = Get<IDictionary<string, string[]>>(env, "owin.ResponseHeaders");
            Assert.NotNull(responseHeaders);
            Assert.Equal("CustomResponseValue", responseHeaders["CustomResponseHeader"].First());
            Assert.Equal(201, Get<int>(env, "owin.ResponseStatusCode"));
        }

        [Fact]
        public void OwinEnvironmentCanBeModified()
        {
            HttpContext context = CreateContext();
            IDictionary<string, object> env = new OwinEnvironment(context);

            env["owin.RequestMethod"] = "SomeMethod";
            env["server.User"] = new ClaimsPrincipal(new ClaimsIdentity("Foo"));
            env["owin.RequestBody"] = Stream.Null;
            var requestHeaders = Get<IDictionary<string, string[]>>(env, "owin.RequestHeaders");
            Assert.NotNull(requestHeaders);
            requestHeaders["CustomRequestHeader"] = new[] { "CustomRequestValue" };
            env["owin.RequestPath"] = "/path";
            env["owin.RequestPathBase"] = "/pathBase";
            env["owin.RequestProtocol"] = "http/1.0";
            env["owin.RequestQueryString"] = "key=value";
            env["owin.RequestScheme"] = "http";
            env["owin.ResponseBody"] = Stream.Null;
            var responseHeaders = Get<IDictionary<string, string[]>>(env, "owin.ResponseHeaders");
            Assert.NotNull(responseHeaders);
            responseHeaders["CustomResponseHeader"] = new[] { "CustomResponseValue" };
            env["owin.ResponseStatusCode"] = 201;

            Assert.Equal("SomeMethod", context.Request.Method);
            Assert.Equal("Foo", context.User.Identity.AuthenticationType);
            Assert.Same(Stream.Null, context.Request.Body);
            Assert.Equal("CustomRequestValue", context.Request.Headers["CustomRequestHeader"]);
            Assert.Equal("/path", context.Request.Path.Value);
            Assert.Equal("/pathBase", context.Request.PathBase.Value);
            Assert.Equal("http/1.0", context.Request.Protocol);
            Assert.Equal("?key=value", context.Request.QueryString.Value);
            Assert.Equal("http", context.Request.Scheme);

            Assert.Same(Stream.Null, context.Response.Body);
            Assert.Equal("CustomResponseValue", context.Response.Headers["CustomResponseHeader"]);
            Assert.Equal(201, context.Response.StatusCode);
        }

        private HttpContext CreateContext()
        {
            var features = new FeatureCollection();
            features.Add(typeof(IHttpRequestFeature), new MoqHttpRequestFeature());
            features.Add(typeof(IHttpResponseFeature), new MoqHttpResponseFeature());
            features.Add(typeof(IHttpAuthenticationFeature), new MoqHttpAuthenticationFeature());
            features.Add(typeof(IHttpRequestLifetimeFeature), new MoqHttpRequestLifetimeFeature());
            return new DefaultHttpContext(features);
        }

        private class MoqHttpRequestFeature : IHttpRequestFeature
        {
            public MoqHttpRequestFeature()
            {
                Headers = new Dictionary<string, string[]>();
            }

            public string Method { get; set; }

            public string Scheme { get; set; }

            public string Protocol { get; set; }

            public Stream Body { get; set; }

            public string PathBase { get; set; }

            public string Path { get; set; }

            public string QueryString { get; set; }

            public IDictionary<string, string[]> Headers { get; set; }
        }

        private class MoqHttpResponseFeature : IHttpResponseFeature
        {
            public MoqHttpResponseFeature()
            {
                Headers = new Dictionary<string, string[]>();
            }

            public Stream Body { get; set; }

            public int StatusCode { get; set; }

            public string ReasonPhrase { get; set; }

            public IDictionary<string, string[]> Headers { get; set; }

            public void OnSendingHeaders(Action<object> callback, object state)
            {
                throw new NotImplementedException();
            }
        }

        private class MoqHttpAuthenticationFeature : IHttpAuthenticationFeature
        {
            public ClaimsPrincipal User { get; set; }

            public IAuthenticationHandler Handler { get; set; }
        }

        private class MoqHttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
        {
            public CancellationToken OnRequestAborted { get; private set; }

            public void Abort()
            {
                throw new NotImplementedException();
            }
        }
    }
}
