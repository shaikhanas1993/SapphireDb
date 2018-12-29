﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata;
using Newtonsoft.Json.Linq;
using RealtimeDatabase.Attributes;
using RealtimeDatabase.Models.Commands;
using RealtimeDatabase.Models.Prefilter;
using RealtimeDatabase.Models.Responses;
using RealtimeDatabase.Websocket;
using RealtimeDatabase.Websocket.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeDatabase.Internal
{
    static class ModelHelper
    {
        public static object[] GetPrimaryKeyValues(this Type type, RealtimeDbContext db, Dictionary<string, JValue> primaryKeys)
        {
            return type.GetPrimaryKeys(db)
                .Select(p => primaryKeys[p.Name.ToCamelCase()].ToObject(p.ClrType)).ToArray();
        }

        public static object[] GetPrimaryKeyValues(this Type type, RealtimeDbContext db, object entityObject)
        {
            return type.GetPrimaryKeys(db)
                .Select(p => p.PropertyInfo.GetValue(entityObject)).ToArray();
        }

        public static string[] GetPrimaryKeyNames(this Type type, RealtimeDbContext db)
        {
            return type.GetPrimaryKeys(db).Select(p => p.Name.ToCamelCase()).ToArray();
        }

        public static IProperty[] GetPrimaryKeys(this Type type, RealtimeDbContext db)
        {
            return db.Model.FindEntityType(type.FullName).FindPrimaryKey().Properties.ToArray();
        }

        public static void UpdateFields(this Type entityType, object entityObject, object newValues, RealtimeDbContext db, WebsocketConnection websocketConnection)
        {
            string[] primaryKeys = entityType.GetPrimaryKeyNames(db);

            if (entityType.GetCustomAttribute<UpdatableAttribute>() != null)
            {
                foreach (PropertyInfo pi in entityType.GetProperties())
                {
                    if (!primaryKeys.Contains(pi.Name.ToCamelCase()))
                    {
                        if (pi.CanUpdate(websocketConnection, entityObject))
                        {
                            pi.SetValue(entityObject, pi.GetValue(newValues));
                        }
                    }
                }
            }
            else
            {
                foreach (PropertyInfo pi in entityType.GetProperties())
                {
                    if (pi.GetCustomAttribute<UpdatableAttribute>() != null)
                    {
                        if (!primaryKeys.Contains(pi.Name.ToCamelCase()))
                        {
                            if (pi.CanUpdate(websocketConnection, entityObject))
                            {
                                pi.SetValue(entityObject, pi.GetValue(newValues));
                            }
                        }
                    }
                }
            }
        }

        public static async Task<Dictionary<string, object>> GenerateUserData(IdentityUser identityUser, AuthDbContextTypeContainer typeContainer, object usermanager)
        {
            Dictionary<string, object> userData = new Dictionary<string, object>();
            Type t = identityUser.GetType();

            IEnumerable<PropertyInfo> properties =
                t.GetProperties().Where(p => p.GetCustomAttribute<AuthUserInformationAttribute>() != null
                || p.Name == "Id" || p.Name == "UserName" || p.Name == "Email");

            foreach (PropertyInfo property in properties)
            {
                if (property.Name != "Roles")
                {
                    userData[property.Name] = property.GetValue(identityUser);
                }
                else
                {
                    userData["_Roles"] = property.GetValue(identityUser);
                }
            }

            userData["Roles"] =
                await(dynamic)typeContainer.UserManagerType.GetMethod("GetRolesAsync").Invoke(usermanager, new object[] { identityUser });

            return userData;
        }

        public static IEnumerable<Dictionary<string, object>> GetUsers(IRealtimeAuthContext db,
            AuthDbContextTypeContainer typeContainer, object usermanager)
        {
            IEnumerable<IdentityUser> users = (IQueryable<IdentityUser>)typeContainer
                .UserManagerType.GetProperty("Users").GetValue(usermanager);

            
            IEnumerable<Dictionary<string, object>> usersConverted = users
                .Select(u => GenerateUserData(u, typeContainer, usermanager).Result);

            return usersConverted;
        }

        public static Dictionary<string, object> GenerateRoleData(IdentityRole identityRole, 
            IEnumerable<IdentityUserRole<string>> userRoles = null)
        {
            return new Dictionary<string, object>
            {
                ["Id"] = identityRole.Id,
                ["Name"] = identityRole.Name,
                ["NormalizedName"] = identityRole.NormalizedName,
                ["UserIds"] = userRoles?.Where(ur => ur.RoleId == identityRole.Id).Select(ur => ur.UserId)
            };
        }

        public static IEnumerable<Dictionary<string, object>> GetRoles(IRealtimeAuthContext db)
        {
            IEnumerable<IdentityUserRole<string>> userRoles = db.UserRoles;

            return db.Roles.Select(r => GenerateRoleData(r, userRoles));
        }
    }
}
