using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace TWFYAPI
{
    public class TWFYAPI : System.Web.UI.Page
    {
        private String api_key;

        // Default constructors
        public TWFYAPI()
        {
        }

        public TWFYAPI(String api_key)
        {
            // Check and set API key
            if (api_key == null || api_key == "") {
                throw new Exception("ERROR: No API key provided.");
            }

            if (!new Regex("^[A-Za-z0-9]+$").IsMatch(api_key)) {
                throw new Exception("ERROR: Invalid API key provided.");
            }

            this.api_key = api_key;
        }

        // Send an API query
        public String query(String func, String[] args)
        {
            // Exit if any arguments are not defined
            if (func == null || func == "" || args == null) {
                throw new Exception("ERROR: Function name or arguments not provided.");
            }

            // Construct the query
            TWFYAPI_Request query = new TWFYAPI_Request(func, args, this.api_key);

            // Execute the query
            return this._execute_query(query);
        }

        // Execute an API query
        private String _execute_query(TWFYAPI_Request query)
        {
            // Make the final URL
            String url = query.encode_arguments();

            // Get the result
            StringBuilder result = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.UserAgent = "TheyWorkForYou.com API C# interface (+https://github.com/rubenarakelyan/twfyapi-aspnet)";
            request.MaximumAutomaticRedirections = 1;
            request.AllowAutoRedirect = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            int count = 0;

            do {
                count = responseStream.Read(buf, 0, buf.Length);

                if (count != 0) {
                    result.Append(Encoding.ASCII.GetString(buf, 0, count));
                }
            } while (count > 0);

            return result.ToString();
        }
    }

    public class TWFYAPI_Request
    {
        private String url = "http://www.theyworkforyou.com/api/";
        private String func;
        private String[] args;
        private String api_key;

        // Default constructor
        public TWFYAPI_Request(String func, String[] args, String api_key)
        {
            // Set function, arguments and API key
            this.func = func;
            this.args = args;
            this.api_key = api_key;

            // Get and set the URL
            this.url = this._get_uri_for_function(this.func);

            // Check to see if valid URL has been set
            if (this.url == null || this.url == "") {
                throw new Exception("ERROR: Invalid function: " + this.func + ". Please look at the documentation for supported functions.");
            }
        }

        // Encode function arguments into a URL query string
        public String encode_arguments()
        {
            // Validate the output argument if it exists
            for (int i = 0; i < this.args.Length; i++) {
                if (this.args[i].Split(':')[0] == "output") {
                    if (!this._validate_output_argument(this.args[i])) {
                        throw new Exception("ERROR: Invalid output type: " + this.args[i] + ". Please look at the documentation for supported output types.");
                    }
                    break;
                }
            }

            // Make sure all mandatory arguments for a particular function are present
            if (!this._validate_arguments(this.func, this.args)) {
                throw new Exception("ERROR: All mandatory arguments for " + this.func + " not provided.");
            }

            // Assemble the URL
            String full_url = this.url + "?key=" + this.api_key + "&";

            foreach (String name in this.args) {
                full_url += name.Split(':')[0] + "=" + Server.UrlEncode(name.Split(':')[1]) + "&";
            }

            full_url.Substring(0, full_url.Length - 1);

            return full_url;
        }

        // Get the URL for a particular function
        private String _get_uri_for_function(String func)
        {
            // Exit if any arguments are not defined
            if (func == null || func == "") {
                return "";
            }

            // Define valid functions
            String[,] valid_functions = {
                                            {
                                                "convertURL",
                                                "getConstituency",
                                                "getConstituencies",
                                                "getPerson",
                                                "getMP",
                                                "getMPInfo",
                                                "getMPsInfo",
                                                "getMPs",
                                                "getLord",
                                                "getLords",
                                                "getMLA",
                                                "getMLAs",
                                                "getMSP",
                                                "getMSPs",
                                                "getGeometry",
                                                "getBoundary",
                                                "getCommittee",
                                                "getDebates",
                                                "getWrans",
                                                "getWMS",
                                                "getHansard",
                                                "getComments",
                                            },
                                            {
                                                "Converts a parliament.uk URL into a TheyWorkForYou one, if possible",
                                                "Searches for a constituency",
                                                "Returns list of constituencies",
                                                "Returns main details for a person",
                                                "Returns main details for an MP",
                                                "Returns extra information for a person",
                                                "Returns extra information for one or more people",
                                                "Returns list of MPs",
                                                "Returns details for a Lord",
                                                "Returns list of Lords",
                                                "Returns details for an MLA",
                                                "Returns list of MLAs",
                                                "Returns details for an MSP",
                                                "Returns list of MSPs",
                                                "Returns centre, bounding box of constituencies",
                                                "Returns boundary polygon of UK Parliament constituency",
                                                "Returns members of Select Committee",
                                                "Returns Debates (either Commons, Westminster Hall, or Lords)",
                                                "Returns Written Answers",
                                                "Returns Written Ministerial Statements",
                                                "Returns any of the above",
                                                "Returns comments",
                                            }
                                        };

            // If the function exists, return its URL
            foreach (String name in valid_functions) {
                if (func == name) {
                    return this.url + func;
                }
            }

            return "";
        }

        // Validate the "output" argument
        private bool _validate_output_argument(String output)
        {
            // Exit if any arguments are not defined
            if (output == null || output == "") {
                return false;
            }

            // Define valid output types
            String[,] valid_params = {
                                         {
                                             "xml",
                                             "php",
                                             "js",
                                             "rabx",
                                         },
                                         {
                                             "XML output",
                                             "Serialized PHP",
                                             "a JavaScript object",
                                             "RPC over Anything But XML",
                                         }
                                     };

            // Check to see if the output type provided is valid
            foreach (String name in valid_params) {
                if (output.Split(':')[1] == name) {
                    return true;
                }
            }

            return false;
        }

        // Validate arguments
        private bool _validate_arguments(String func, String[] args)
        {
            // Define manadatory arguments
            String[,] functions_params = {
                                             {
                                                "convertURL",
                                                "getConstituency",
                                                "getConstituencies",
                                                "getPerson",
                                                "getMP",
                                                "getMPInfo",
                                                "getMPs",
                                                "getLord",
                                                "getLords",
                                                "getMLA",
                                                "getMLAs",
                                                "getMSPs",
                                                "getGeometry",
                                                "getBoundary",
                                                "getCommittee",
                                                "getDebates",
                                                "getWrans",
                                                "getWMS",
                                                "getHansard",
                                                "getComments",
                                             },
                                             {
                                                 "url",
                                                 "postcode",
                                                 "",
                                                 "id",
                                                 "",
                                                 "id",
                                                 "",
                                                 "id",
                                                 "",
                                                 "",
                                                 "",
                                                 "",
                                                 "",
                                                 "name",
                                                 "name",
                                                 "type",
                                                 "",
                                                 "",
                                                 "",
                                                 "",
                                             }
                                         };

            // Check to see if all mandatory arguments are present
            for (int i = 0; i < functions_params.Length / 2; i++) {
                if (functions_params[0, i] == func) {
                    if (functions_params[1, i] != "") {
                        String[] required_params = functions_params[1, i].Split(',');
                        bool isset = false;

                        foreach (String param in required_params) {
                            for (int k = 0; k < args.Length; k++) {
                                if (args[k].Split(':')[0] == param) {
                                    isset = true;
                                }
                            }

                            if (!isset) {
                                return false;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
