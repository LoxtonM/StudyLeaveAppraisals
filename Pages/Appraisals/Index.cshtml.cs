using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyLeaveAppraisals.Data;
using StudyLeaveAppraisals.Meta;
using StudyLeaveAppraisals.Models;
using System.Globalization;

namespace StudyLeaveAppraisals.Pages.Appraisals
{
    public class IndexModel : PageModel
    {
        private readonly DataContext _context;
        private Metadata meta;
        private PrintServices printer;
        public IndexModel(DataContext context)
        {
            _context = context;
            meta = new Metadata(_context);
            printer = new PrintServices();
        }

        public List<StaffMembers> staffMembers { get; set; }
        public List<Appointments> appointments { get; set; }
        public List<Appointments> mdcs { get; set; }
        public List<Appointments> totalappts { get; set; }
        public List<Appointments> apptsPerClinic { get; set; }        
        public string staffCode { get; set; }
        public string staffName { get; set; }
        public bool isSupervisor { get; set; }
        public string sClinCode;
        public DateTime startDate;
        public DateTime endDate;
        public int iPatientsSeen;
        public int iPatientsSeenByAnother;
        public int iCancellations;
        public int iDNAs;
        public int iNotRecorded;
        public int iClinicsHeld;
        public int iTotalAppointments;

        public bool isSuccess;
        public string sMessage;

        [Authorize]
        public void OnGet(string? clinicianCode, DateTime? dStart, DateTime? dEnd)
        {
            try
            {
                isSupervisor = false;
                if (User.Identity.Name is null)
                {
                    Response.Redirect("Login");
                }
                else
                {
                    staffName = meta.GetStaffName(User.Identity.Name);
                    staffCode = meta.GetStaffCode(User.Identity.Name);                    
                    isSupervisor = meta.GetIsConsSupervisor(staffCode);
                }

                if(clinicianCode == null)
                {
                    clinicianCode = staffCode;
                }

                staffMembers = meta.GetStaffMembers();
                
                sClinCode = clinicianCode;
                if (dStart == null)
                {
                    dStart = DateTime.Now.AddDays(-365);
                }
                if (dEnd == null)
                {
                    dEnd = DateTime.Now;
                }
                //Data
                appointments = meta.GetAppointments(clinicianCode, dStart, dEnd);
                mdcs = meta.GetMDC(clinicianCode, dStart, dEnd);
                totalappts = appointments.Concat(mdcs).OrderBy(a => a.BOOKED_DATE).ThenBy(a => a.BOOKED_TIME).ToList();
                
                

                //Numbers
                startDate = dStart.GetValueOrDefault();
                endDate = dEnd.GetValueOrDefault();
                iPatientsSeen = totalappts.Where(a => a.Attendance == "Attended" && a.SeenBy == a.STAFF_CODE_1).Count();
                iPatientsSeenByAnother = totalappts.Where(a => a.Attendance == "Attended" && a.SeenBy != a.STAFF_CODE_1).Count();
                iCancellations = totalappts.Where(a => a.Attendance.Contains("Canc")).Count();
                iDNAs = totalappts.Where(a => a.Attendance == "Did not attend").Count();
                iNotRecorded = totalappts.Where(a => a.Attendance == "NOT RECORDED").Count();
                iTotalAppointments = totalappts.Count();
                iClinicsHeld = totalappts.DistinctBy(a => a.BOOKED_DATE).Count();
                
            }
            catch (Exception ex)
            {
                Response.Redirect("Error?sError=" + ex.Message);
            }
        }
        public void OnPost(string? clinicianCode, DateTime? dStart, DateTime? dEnd, bool? isPrintReq = false)
        {
            try
            {
                staffName = meta.GetStaffName(User.Identity.Name);
                staffCode = meta.GetStaffCode(User.Identity.Name);                
                isSupervisor = meta.GetIsConsSupervisor(staffCode);
                staffMembers = meta.GetStaffMembers();
                appointments = meta.GetAppointments(clinicianCode, dStart, dEnd);
                mdcs = meta.GetMDC(clinicianCode, dStart, dEnd);
                totalappts = appointments.Concat(mdcs).OrderBy(a => a.BOOKED_DATE).ThenBy(a => a.BOOKED_TIME).ToList();

                sClinCode = clinicianCode;
                string sClinName = meta.GetStaffNameFromStaffCode(sClinCode);                
                
                startDate = dStart.GetValueOrDefault();
                endDate = dEnd.GetValueOrDefault();
                iPatientsSeen = totalappts.Where(a => a.Attendance == "Attended" && a.SeenBy == a.STAFF_CODE_1).Count();
                iPatientsSeenByAnother = totalappts.Where(a => a.Attendance == "Attended" && a.SeenBy != a.STAFF_CODE_1).Count();
                iCancellations = totalappts.Where(a => a.Attendance.Contains("Canc")).Count();
                iDNAs = totalappts.Where(a => a.Attendance == "Did not attend").Count();
                iNotRecorded = totalappts.Where(a => a.Attendance == "NOT RECORDED").Count();
                iTotalAppointments = totalappts.Count();
                iClinicsHeld = totalappts.DistinctBy(a => a.BOOKED_DATE).Count();

                if (isPrintReq.GetValueOrDefault())
                {
                    printer.PrintReport(totalappts, sClinName, dStart, dEnd);
                    //isSuccess = true;
                    //sMessage = "The report has been saved to your C:\\CGU_DB folder.";
                    
                    Response.Redirect("Download?sClin=" + sClinName + "&dStart=" + dStart.Value.ToString("yyyy-MM-dd") + "&dEnd=" + dEnd.Value.ToString("yyyy-MM-dd"));
                    
                }

            }
            catch (Exception ex)
            {
                Response.Redirect("Error?sError=" + ex.Message);
            }
        }

        
    }
}
