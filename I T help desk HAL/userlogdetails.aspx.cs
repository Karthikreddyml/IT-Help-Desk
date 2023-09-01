﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace I_T_help_desk_HAL
{
    public partial class WebForm8 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            LoadAndDisplayComments();

            if (!IsPostBack)
            {
              
                // Check if the LogID exists in the query string
                if (Request.QueryString["LogID"] != null)
                {
                    // Get the LogID from the query string
                    string logID = Request.QueryString["LogID"];

                    // Fetch the log details from the database using the LogID
                    DataTable dtLogDetails = GetLogDetailsFromDatabase(logID);

                    // Check if any log details are retrieved
                    if (dtLogDetails != null && dtLogDetails.Rows.Count > 0)
                    {
                        // Display the log details in the corresponding text boxes
                        TextBox1.Text = dtLogDetails.Rows[0]["FullName"].ToString();
                        TextBox2.Text = dtLogDetails.Rows[0]["Contact"].ToString();
                        TextBox3.Text = dtLogDetails.Rows[0]["Department"].ToString();
                        TextBox4.Text = dtLogDetails.Rows[0]["Location"].ToString();
                        TextBox10.Text = dtLogDetails.Rows[0]["Equipment"].ToString();
                        TextBox6.Text = dtLogDetails.Rows[0]["Problem"].ToString();
                        TextBox7.Text = dtLogDetails.Rows[0]["CreatedDate"].ToString();
                        TextBox5.Text = dtLogDetails.Rows[0]["LastDate"].ToString();
                        TextBox11.Text = dtLogDetails.Rows[0]["TechnicianName"].ToString();
                        TextBox12.Text = dtLogDetails.Rows[0]["TakenDate"].ToString();
                        txtParagraph.Text = dtLogDetails.Rows[0]["Message"].ToString();
                    }
                }
            }
        }



        private void LoadAndDisplayComments()
        {
            if (!string.IsNullOrEmpty(Request.QueryString["LogID"]))
            {
                string logId = Request.QueryString["LogID"];
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT C.CommentId, COALESCE(A.FullName, U.FullName, T.FullName) AS CommenterFullName, C.CommentText, C.CommentDate, C.ParentcommentId FROM  Comment_table C LEFT JOIN  Admins_table A ON C.UserId = A.AdminId LEFT JOIN   Users_table U ON C.UserId = U.UserId LEFT JOIN Technicians_Table T ON C.UserId = T.TechnicianId WHERE  C.LogId = @LogId ORDER BY C.CommentDate DESC";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@LogId", logId);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            comments.InnerHtml = ""; // Clear previous comments

                            while (reader.Read())
                            {
                                int commentId = Convert.ToInt32(reader["CommentId"]);
                                string userName = reader["CommenterFullName"].ToString();
                                string commentText = reader["CommentText"].ToString();
                                DateTime commentDate = Convert.ToDateTime(reader["CommentDate"]);
                                int? parentcommentId = reader["ParentcommentId"] != DBNull.Value ? Convert.ToInt32(reader["ParentCommentId"]) : (int?)null;

                                string commentHtml = $"<li class='comment'><div class='user'>{userName}</div><div class='date'>{commentDate}</div><div class='text'>{commentText} <hr></div> ";



                                commentHtml += "</li>";

                                comments.InnerHtml += commentHtml;
                            }
                        }
                    }
                }
            }
        }
        private DataTable GetLogDetailsFromDatabase(string logID)
        {
            // Fetch the connection string from the web.config file
            string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

            // Create the SQL query to fetch log details from the Logs_table based on LogID
            string query = "SELECT FullName, Contact, Department, Location, Equipment, Problem, CreatedDate, LastDate, TechnicianName, TakenDate, Message FROM Logs_table WHERE LogID = @LogID";

            // Set up the connection and command
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Add the LogID parameter to the command
                    command.Parameters.AddWithValue("@LogID", logID);

                    // Create a DataTable to store the results
                    DataTable dtLogDetails = new DataTable();

                    // Open the connection, execute the query, and fill the DataTable
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        dtLogDetails.Load(reader);
                    }
                    connection.Close();

                    return dtLogDetails;
                }
            }
        }

        protected void Button1_Click(object sender, EventArgs e)
        {
            // Check if the LogID exists in the query string
            if (Request.QueryString["LogID"] != null)
            {
                // Get the LogID from the query string
                string logID = Request.QueryString["LogID"];

                // Fetch the connection string from the web.config file
                string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                // Update the SolvedDate and CaseStatus in the Logs_table for the specified LogID
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Update SolvedDate to the current date and time
                    string updateSolvedDateQuery = "UPDATE Logs_table SET SolvedDate = @SolvedDate WHERE LogID = @LogID";
                    using (SqlCommand command = new SqlCommand(updateSolvedDateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@SolvedDate", DateTime.Now);
                        command.Parameters.AddWithValue("@LogID", logID);
                        command.ExecuteNonQuery();
                    }

                    // Update CaseStatus to "Solved"
                    string updateCaseStatusQuery = "UPDATE Logs_table SET CaseStatus = @CaseStatus WHERE LogID = @LogID";
                    using (SqlCommand command = new SqlCommand(updateCaseStatusQuery, connection))
                    {
                        command.Parameters.AddWithValue("@CaseStatus", "Solved");
                        command.Parameters.AddWithValue("@LogID", logID);
                        command.ExecuteNonQuery();
                    }

                    connection.Close();
                }

                // Redirect to userhome.aspx
                Response.Redirect("userhome.aspx");
            }
        }

        protected void addCommentButton_Click1(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(Request.QueryString["LogID"]))
            {
                string logId = Request.QueryString["LogID"];
                int userId = Convert.ToInt32(Session["UserID"]); // Retrieve UserID from session
                string commentText = TextBox9.Text.Trim();

                if (!string.IsNullOrEmpty(commentText))
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["con"].ConnectionString;

                    using (SqlConnection con = new SqlConnection(connectionString))
                    {
                        con.Open();

                        // Generate a unique 4-digit number for the CommentID
                        string commentId = GenerateUniqueCommentID(con);

                        string query = "INSERT INTO Comment_table (CommentID, LogId, UserId, CommentText, CommentDate, ParentcommentId) VALUES (@CommentID, @LogId, @UserId, @CommentText, @CommentDate, @ParentcommentId)";
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.Parameters.AddWithValue("@CommentID", commentId);
                            cmd.Parameters.AddWithValue("@LogId", logId);
                            cmd.Parameters.AddWithValue("@UserId", userId);
                            cmd.Parameters.AddWithValue("@CommentText", commentText);
                            cmd.Parameters.AddWithValue("@CommentDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ParentcommentId", DBNull.Value);

                            cmd.ExecuteNonQuery();
                        }
                    }

                    TextBox9.Text = ""; // Clear the input field
                    LoadAndDisplayComments();
                }
            }
        }

        private string GenerateUniqueCommentID(SqlConnection connection)
        {
            // Generate a unique 4-digit number using NEWID() and GUID
            using (SqlCommand cmd = new SqlCommand("SELECT LEFT(ABS(CHECKSUM(NEWID())), 4)", connection))
            {
                return cmd.ExecuteScalar().ToString();
            }
        }

    }
}