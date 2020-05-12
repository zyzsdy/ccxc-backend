using Ccxc.Core.DbOrm;
using ccxc_backend.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace ccxc_backend.DataServices
{
    public class DbMaintenance
    {
        public SqlBaseClient DbBase;
        public string DbConnStr;

        public DbMaintenance(string connStr)
        {
            DbConnStr = connStr;
            DbBase = new SqlBaseClient(connStr, SqlSugar.DbType.MySql);
        }

        public void InitDatabase()
        {
            DbBase.DbMaintenance.CreateDatabase();

            new Announcement(DbConnStr).InitTable();
            new AnswerLog(DbConnStr).InitTable();
            new Invite(DbConnStr).InitTable();
            new LoginLog(DbConnStr).InitTable();
            new Message(DbConnStr).InitTable();
            new Progress(DbConnStr).InitTable();
            new Puzzle(DbConnStr).InitTable();
            new PuzzleGroup(DbConnStr).InitTable();
            new User(DbConnStr).InitTable();
            new UserGroup(DbConnStr).InitTable();
            new UserGroupBind(DbConnStr).InitTable();
        }
    }
}
