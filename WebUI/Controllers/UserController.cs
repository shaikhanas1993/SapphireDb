﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebUI.Data;
using WebUI.Data.Models;

namespace WebUI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private RealtimeContext db;

        public UserController(RealtimeContext _db)
        {
            db = _db;
        }

        [HttpGet]
        public List<User> Get()
        {
            return db.Users.ToList();
        }

        [HttpPost]
        public User Post([FromBody]User newUser)
        {
            db.Users.Add(newUser);
            db.Tests.Add(new Test() { Content = newUser.Username });
            db.SaveChanges();
            return newUser;
        }

        [HttpPut()]
        public User Put([FromBody]User newUser)
        {
            db.Users.Update(newUser);
            db.SaveChanges();
            return newUser;
        }

        [HttpDelete()]
        public User Delete([FromBody]User newUser)
        {
            db.Users.Remove(newUser);
            db.SaveChanges();
            return newUser;
        }
    }
}