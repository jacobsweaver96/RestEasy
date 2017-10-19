using log4net;
using RestEasy.Attributes;
using RestEasy.Services;
using SandyModels.Models.ApiModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using SandyModels.Models;
using SandyUtils.Utils;

namespace RestEasy.Controllers
{
    public abstract class RestController : ApiController
    {
        /// <summary>
        /// The query parameter key name for indicating whether or not endpoint details should be included
        /// Defaults to 'includeEndpoints'
        /// </summary>
        protected virtual string IncludeEndpointParamKey
        {
            get { return "includeEndpoints"; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected RestController() { }

        /// <summary>
        /// Constructor that injects authorization service
        /// </summary>
        /// <param name="authorizationService">Authorization service</param>
        protected RestController(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Constructor that injects logger
        /// </summary>
        /// <param name="log">Logger</param>
        protected RestController(ILog log)
        {
            _log = log;
        }

        /// <summary>
        /// Constructor that injects authorization service and logger
        /// </summary>
        /// <param name="authorizationService">Authorization service</param>
        /// <param name="log">Logger</param>
        protected RestController(IAuthorizationService authorizationService, ILog log)
        {
            _log = log;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Gets the endpoints set in this controller
        /// </summary>
        /// <returns></returns>
        public abstract ApiResponse Paths();

        /// <summary>
        /// Endpoints that are related to the endpoint of the current http context 
        /// </summary>
        protected abstract List<RestController> RelatedEndpoints { get; }

        private IAuthorizationService _authorizationService;
        /// <summary>
        /// The service used for authorization
        /// </summary>
        protected IAuthorizationService AuthorizationService
        {
            get
            {
                if (_authorizationService == null)
                {
                    return DependencyResolver.Resolve<IAuthorizationService>();
                }

                return _authorizationService;
            }
        }

        private ILog _log;
        /// <summary>
        /// The logger
        /// </summary>
        protected ILog Log
        {
            get
            {
                if (_log == null)
                {
                    return DependencyResolver.Resolve<ILog>();
                }

                return _log;
            }
        }

        /// <summary>
        /// Creates a response consisting solely of routing information
        /// </summary>
        /// <returns>An API formatted response</returns>
        protected ApiResponse CreateRestResponse()
        {
            ApiResponse response = new ApiResponse();

            try
            {
                response.EndPointItems = GetRelatedRoutes();
            }
            catch (Exception ex)
            {
                Log.Error($"Exception while getting related routing information", ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            return response;
        }

        /// <summary>
        /// Create a response without a return type
        /// </summary>
        /// <param name="callingAttributes">The calling methods attributes</param>
        /// <param name="dataExec">The data interaction function</param>
        /// <returns>An API formatted response</returns>
        protected async Task<ApiResponse> CreateRestResponse(IList<CustomAttributeData> callingAttributes, Func<Task<DataResponse>> dataExec)
        {
            var tResponse = await CreateRestResponse<object, object>(callingAttributes, dataExec);
            return new ApiResponse
            {
                EndPointItems = tResponse?.EndPointItems ?? new List<EndPointItem>()
            };
        }

        /// <summary>
        /// Creates a response that returns data to the client
        /// </summary>
        /// <typeparam name="T">The type of data returned to the client</typeparam>
        /// <typeparam name="U">The type of serializable data returned to the client</typeparam>
        /// <param name="callingAttributes">The attributes of the calling method</param>
        /// <param name="dataExec">Function for getting data as a database model</param>
        /// <param name="transExec">Function for tranforming the database model to a serializable model</param>
        /// <returns>An API formatted response</returns>
        protected async Task<ApiResponse<U>> CreateRestResponse<T, U>(IList<CustomAttributeData> callingAttributes,
            Func<Task<DataResponse>> dataExec, Func<T, U> transExec = null) where U : new()
        {
            ApiResponse<U> response = new ApiResponse<U>();
            DataResponse ret;

            List<PermissionLevel> requiredPermissions = new List<PermissionLevel>();

            foreach (var v in callingAttributes)
            {
                if (v.AttributeType == typeof(RequiresReadAccessAttribute))
                {
                    requiredPermissions.Add(PermissionLevel.READ);
                    continue;
                }
                if (v.AttributeType == typeof(RequiresWriteAccessAttribute))
                {
                    requiredPermissions.Add(PermissionLevel.WRITE);
                    continue;
                }
                if (v.AttributeType == typeof(RequiresAdminAccessAttribute))
                {
                    requiredPermissions.Add(PermissionLevel.ADMIN);
                    continue;
                }
            }

            try
            {
                var authHeader = HttpContext.Current.Request.Headers["Authorization"];

                // Only allow https
                var isSchemeAllowed = HttpContext.Current.Request.Url.Scheme == Uri.UriSchemeHttps;
                
                if (!isSchemeAllowed)
                {
                    if (HttpContext.Current.Request.Url.Scheme == Uri.UriSchemeHttp &&
                        !String.IsNullOrWhiteSpace(authHeader) && authHeader.Length == 32)
                    {
                        Log.Warn($"Client key beginning with {authHeader.Substring(0, 10)}... was receieved over an insecure connection");
                    }

                    throw new HttpResponseException(HttpStatusCode.Forbidden);
                }
                else if (await AuthorizationService.Authorize(authHeader, requiredPermissions))
                {
                    ret = await dataExec();
                    switch (ret.Status)
                    {
                        case DataStatusCode.SUCCESS:
                            // 200 OK by default
                            break;
                        case DataStatusCode.INVALID:
                            throw new HttpResponseException(HttpStatusCode.BadRequest);
                        case DataStatusCode.ERROR:
                            throw new HttpResponseException(HttpStatusCode.InternalServerError);
                        default:
                            throw new HttpResponseException(HttpStatusCode.NotImplemented);
                    }

                    if (ret is DataResponse<T>)
                    {
                        var typedRet = (DataResponse<T>)ret;
                        response.Content = typedRet.HasValue ? transExec(typedRet.Value) : default(U);
                    }
                }
                else
                {
                    throw new HttpResponseException(HttpStatusCode.Unauthorized);
                }
            }
            catch (HttpResponseException ex)
            {
                // Bubble up
                throw ex;
            }
            catch (Exception ex)
            {
                Log.Error($"Exception occurred while creating an api response", ex);
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            var queryParams = HttpUtility.ParseQueryString(HttpContext.Current.Request.Url.Query);

            if (!queryParams.AllKeys.Any(v => v == IncludeEndpointParamKey)
                || queryParams[IncludeEndpointParamKey]?.ToUpper() == Boolean.TrueString.ToUpper())
            {
                response.EndPointItems = GetRelatedRoutes();
            }

            return response;
        }

        private List<EndPointItem> GetRelatedRoutes(bool shallow = false)
        {
            List<EndPointItem> endpointObjs = new List<EndPointItem>();
            MethodInfo[] infos = GetType().GetMethods();
            Uri requestUri = HttpContext.Current.Request.Url;

            foreach (var v in infos.Where(w => w.IsPublic && w.GetCustomAttribute(typeof(RouteAttribute)) != default(Attribute)))
            {
                RouteAttribute route = v.GetCustomAttribute<RouteAttribute>();
                RestInfoAttribute restInfo = v.GetCustomAttribute<RestInfoAttribute>();
                RoutePrefixAttribute routePrefix = v.DeclaringType.GetCustomAttribute<RoutePrefixAttribute>();

                bool isRouteAbsolute = (route?.Template.FirstOrDefault() == '~');
                string fullRoute = $"{requestUri.Host}:{requestUri.Port}";

                if (routePrefix == null)
                {
                    Log.Warn($"The controller {v.DeclaringType.ToString()} doesn't have a route prefix attribute");
                }
                else if (!isRouteAbsolute && routePrefix.Prefix.Length > 0)
                {
                    string _prefix;
                    if (routePrefix.Prefix[0] == '~')
                    {
                        _prefix = new string(routePrefix.Prefix.Skip(1).ToArray());
                    }
                    else
                    {
                        _prefix = routePrefix.Prefix;
                    }

                    fullRoute += $"/{_prefix}";
                }

                if (route == null)
                {
                    Log.Warn($"The method {v.Name} of the controller {v.DeclaringType.ToString()} doesn't have a route attribute");
                }
                else if (route.Template.Length > 0)
                {
                    string _routeStr;
                    if (route.Template[0] == '~')
                    {
                        _routeStr = new string(route.Template.Skip(1).ToArray());
                    }
                    else
                    {
                        _routeStr = $"/{route.Template}";
                    }

                    fullRoute += $"{_routeStr}";
                }

                if (restInfo != null)
                {
                    endpointObjs.Add(new EndPointItem(restInfo.RestHttpMethod, fullRoute, restInfo.RestDescription, restInfo.RequiresModel ? restInfo.RestModel : null));
                }
            }

            if (!shallow)
            {
                foreach (var endpoint in RelatedEndpoints)
                {
                    try
                    {
                        endpoint.ActionContext = ActionContext;
                        endpointObjs.AddRange(endpoint.GetRelatedRoutes(true));
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"There was an exception while getting the endpoint information for the controller {endpoint.GetType().DeclaringType.ToString()}", ex);
                    }
                }
            }

            return endpointObjs;
        }
    }
}
