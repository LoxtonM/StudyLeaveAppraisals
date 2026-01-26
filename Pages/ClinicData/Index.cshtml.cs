using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyLeaveAppraisals.Meta;
using ClinicalXPDataConnections.Meta;
using ClinicalXPDataConnections.Models;
using ClinicalXPDataConnections.Data;

namespace StudyLeaveAppraisals.Pages.ClinicData
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ClinicalContext _context;
        private readonly IAppointmentData _appointmentData;
        private readonly IStaffUserData _staffData;
        private readonly IClinicVenueData _venueData;
        private readonly IOutcomeData _outcomeData;
        private readonly IDiseaseData _diseaseData;
        private readonly DoSQL _sql;
        public IndexModel(ClinicalContext context, IConfiguration config)
        {
            _config = config;
            _context = context;
            _appointmentData = new AppointmentData(_context);
            _staffData = new StaffUserData(_context);
            _venueData = new ClinicVenueData(_context);
            _outcomeData = new OutcomeData(_context);
            _diseaseData = new DiseaseData(_context);
            _sql = new DoSQL(_config);
        }

        public List<StaffMember> staffMembers { get; set; }
        public List<ClinicVenue> clinicVenues { get; set; }
        public List<Appointment> appointments { get; set; }
        public List<Appointment> mdcs { get; set; }
        public List<Appointment> totalappts { get; set; }
        public List<Outcome> outcomes { get; set; }
        public List<Disease> diseases { get; set; }
        public string staffCode { get; set; }
        public string staffName { get; set; }
        public StaffMember staffMember { get; set; }
        public string clinCode;
        public string venCode;
        public string outCome;
        public string seenBy;
        public string disCode;
        public DateTime startDate;
        public DateTime endDate;
        

        public bool isSuccess;
        public string Message;


        [Authorize]
        public void OnGet(string? clinicianCode, string? venueCode, string? outcome, string? seenby, string? diseaseCode, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                IPAddressFinder _ip = new IPAddressFinder(HttpContext);
                if (User.Identity.Name is null)
                {
                    Response.Redirect("Login");
                }
                else
                {                    
                    //staffName = _staffData.GetStaffName(User.Identity.Name);
                    staffMember = _staffData.GetStaffMemberDetails(User.Identity.Name);
                    //staffCode = _staffData.GetStaffCode(User.Identity.Name);
                    staffCode = staffMember.STAFF_CODE;
                    _sql.SqlWriteUsageAudit(staffCode, "", "Clinic Data index", _ip.GetIPAddress());
                }

                if(clinicianCode == null)
                {
                    clinicianCode = staffCode;
                }

                staffMembers = _staffData.GetClinicalStaffList();
                clinicVenues = _venueData.GetVenueList();
                outcomes = _outcomeData.GetOutcomeList();
                diseases = _diseaseData.GetDiseaseList().OrderBy(d => d.DISEASE_CODE).ToList();
                
                clinCode = clinicianCode;
                venCode = venueCode;
                outCome = outcome;
                seenBy = seenby;
                disCode = diseaseCode;

                if (startDate == null)
                {
                    startDate = DateTime.Now.AddDays(-365);
                }
                if (endDate == null)
                {
                    endDate = DateTime.Now;
                }
                //Data
                appointments = _appointmentData.GetAppointmentsByClinicians(clinicianCode, startDate, endDate);
                mdcs = _appointmentData.GetMDC(clinicianCode, startDate, endDate);
                totalappts = appointments.Concat(mdcs).OrderBy(a => a.BOOKED_DATE).ThenBy(a => a.BOOKED_TIME).ToList();
                
                //Numbers
                this.startDate = startDate.GetValueOrDefault();
                this.endDate = endDate.GetValueOrDefault();

                
            }
            catch (Exception ex)
            {
                Response.Redirect("Error?sError=" + ex.Message);
            }
        }
        public void OnPost(string? clinicianCode, string? venueCode, string? outcome, string? seenby, string? diseaseCode, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                IPAddressFinder _ip = new IPAddressFinder(HttpContext);
                staffName = _staffData.GetStaffName(User.Identity.Name);
                staffCode = _staffData.GetStaffCode(User.Identity.Name);  
                staffMember = _staffData.GetStaffMemberDetails(User.Identity.Name);
                _sql.SqlWriteUsageAudit(staffCode, $"Clinician={clinicianCode}", "Clinic Data index", _ip.GetIPAddress());
                staffMembers = _staffData.GetStaffMemberList();
                clinicVenues = _venueData.GetVenueList();
                outcomes = _outcomeData.GetOutcomeList();
                diseases = _diseaseData.GetDiseaseList().OrderBy(d => d.DISEASE_CODE).ToList();

                if (clinicianCode != null)
                {
                    staffCode = clinicianCode;
                }
                
                appointments = _appointmentData.GetAppointmentsByClinicians(staffCode, startDate, endDate);
                mdcs = _appointmentData.GetMDC(staffCode, startDate, endDate);
                totalappts = appointments.Concat(mdcs).OrderBy(a => a.BOOKED_DATE).ThenBy(a => a.BOOKED_TIME).ToList();

                if(venueCode != null)
                {
                    totalappts = totalappts.Where(a => a.FACILITY.ToUpper() == venueCode.ToUpper()).ToList();
                }

                if (outcome != null)
                {
                    totalappts = totalappts.Where(a => a.Attendance.ToUpper() == outcome.ToUpper()).ToList();
                }

                if(seenby != null)
                {
                    totalappts = totalappts.Where(a => a.SeenBy != null).ToList();
                    totalappts = totalappts.Where(a => a.SeenBy.ToUpper() == seenby.ToUpper() || 
                        (a.SeenBy2 != null && a.SeenBy2.ToUpper() == seenby.ToUpper()) ||
                        (a.SeenBy3 != null && a.SeenBy3.ToUpper() == seenby.ToUpper())).ToList();
                }

                if (diseaseCode != null)
                {
                    List<Patient> patients = new List<Patient>();
                    IPatientData patData = new PatientData(_context);
                    foreach(var a in totalappts)
                    {
                        patients.Add(patData.GetPatientDetails(a.MPI));
                    }
                    List<Diagnosis> diags = new List<Diagnosis>();
                                       
                    foreach (var pat in patients)
                    {                        
                        List<Diagnosis> diagsPerPatient = _diseaseData.GetDiseaseListByPatient(pat.MPI);
                        foreach (var diagnosis in diagsPerPatient)
                        {
                            if (diagnosis.DISEASE_CODE == diseaseCode)
                            {
                                diags.Add(diagnosis);
                            }
                        }
                    }

                    List<Appointment> actualAppts = new List<Appointment>();

                    foreach(var app in totalappts)
                    {
                        if(diags.Where(d => d.MPI == app.MPI).Count() > 0)
                        {
                            actualAppts.Add(app);
                        }
                    }

                    totalappts = actualAppts;
                }
                                
                string clinName = _staffData.GetStaffNameFromStaffCode(staffCode);

                clinCode = clinicianCode;
                venCode = venueCode;
                outCome = outcome;
                seenBy = seenby;
                disCode = diseaseCode;

                this.startDate = startDate.GetValueOrDefault();
                this.endDate = endDate.GetValueOrDefault();
                
            }
            catch (Exception ex)
            {
                Response.Redirect("Error?sError=" + ex.Message);
            }
        }

        
    }
}
