using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestEasy.Attributes
{
    /// <summary>
    /// Attribute for relaying endpoint information to an API client 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RestInfoAttribute : Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">HTTP method name</param>
        /// <param name="desc">Endpoint description</param>
        public RestInfoAttribute(string method, string desc)
        {
            RestHttpMethod = method;
            RestDescription = desc;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="method">HTTP method name</param>
        /// <param name="desc">Endpoint description</param>
        /// <param name="modelName">The name of the model required by this endpoint</param>
        public RestInfoAttribute(string method, string desc, string modelName)
        {
            RestHttpMethod = method;
            RestDescription = desc;

            if (!String.IsNullOrWhiteSpace(modelName))
            {
                RequiresModel = true;
                RestModel = modelName;
            }
        }

        /// <summary>
        /// The endpoint's HTTP method
        /// </summary>
        public string RestHttpMethod { get; private set; }

        /// <summary>
        /// The endpoint's description
        /// </summary>
        public string RestDescription { get; private set; }

        /// <summary>
        /// Whether or not the endpoint expects a model in the request body
        /// </summary>
        public bool RequiresModel { get; private set; }

        /// <summary>
        /// The name of the model expected by the endpoint
        /// </summary>
        public string RestModel { get; private set; }
    }
}
