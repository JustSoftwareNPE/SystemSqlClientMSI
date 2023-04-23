using HarmonyLib;
using System;
using System.Reflection;
using System.Data.SqlClient;
using Azure.Identity;
using Azure.Core;
using System.Linq;
using Hangfire.Annotations;
using Microsoft.Practices.EnterpriseLibrary.Data;

/* Copyright 2023 - Just Software
 * Use or distribution for other projects is not allowed 
 * without written agreement by Just Software.
 * Check also our products on our website: https://just-software.dk/
 *   - EasyScep - the simple, cost-effective certificate authority for Microsoft Intune
 *   - EasyRadius - the easy middleman for doing EAP-TLS certificate authentication with Radius
 */

namespace SQLManagedIdentityHelper
{
    public class SqlConnectionPatcher
    {
        public static SqlConnection CreateSqlConnectionWithAccessToken(string connectionString)
        {
            SqlConnection conn = null;
            var connStrBuilder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(connStrBuilder.Password) && string.IsNullOrWhiteSpace(connStrBuilder.UserID))
            {
                string token = GetAccessToken();
                conn = new SqlConnection(connectionString)
                {
                    AccessToken = token
                };

            }
            else
            {
                conn = new SqlConnection(connectionString);
            }
            return conn;
        }

        private static string GetAccessToken()
        {
            // No password given in connection string - expect managed identity
            AccessToken accessToken = new DefaultAzureCredential(
                new DefaultAzureCredentialOptions
                {
                    //                    TenantId = #FIXME - ADD TenantID or remove to use defualt
                }
                ).GetToken(
                new TokenRequestContext(new string[] { "https://database.windows.net//.default" }));
            var token = accessToken.Token;
            return token;
        }

        public static void PatchSqlConnection()
        {
            var harmony = new Harmony("net.just-cloud.sqlconnection.patcher");

            var constPreSqlOpen = SymbolExtensions.GetMethodInfo((object __instance) => SqlConnection_PreOpenHook(__instance));

            var s = new SqlConnection(); // Ensure we have loaded library
            var targetTypeSql = AccessTools.TypeByName("SqlConnection");
            var targetSqlOpenMethods = targetTypeSql.GetMethods();
            foreach (var tm in targetSqlOpenMethods.Where(q => q.Name.StartsWith("Open")))
            {
                if (tm.IsDeclaredMember())
                {
                    harmony.Patch(tm, new HarmonyMethod(constPreSqlOpen), null);
                }
            }
        }
        static void SqlConnection_PreOpenHook(object __instance)
        {
            var sqlconn = __instance as SqlConnection;
            if (sqlconn.AccessToken == null)
            {
                // Only add access token if userid / password not present in connection string
                var connStrBuilder = new SqlConnectionStringBuilder(sqlconn.ConnectionString);
                if (string.IsNullOrWhiteSpace(connStrBuilder.Password) && string.IsNullOrWhiteSpace(connStrBuilder.UserID))
                    sqlconn.AccessToken = GetAccessToken();
            }
        }
    }
}
