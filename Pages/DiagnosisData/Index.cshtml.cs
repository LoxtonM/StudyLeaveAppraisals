using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyLeaveAppraisals.Meta;
using ClinicalXPDataConnections.Meta;
using ClinicalXPDataConnections.Models;
using ClinicalXPDataConnections.Data;

namespace StudyLeaveAppraisals.Pages.DiagnosisData
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ClinicalContext _context;        
        private readonly IDiseaseDataAsync _diseaseData;
        private readonly IStaffUserDataAsync _staffData;
        private readonly DoSQL _sql;
        public IndexModel(ClinicalContext context, IConfiguration config)
        {
            _config = config;
            _context = context;
            _diseaseData = new DiseaseDataAsync(_context);
            _staffData = new StaffUserDataAsync(_context);
            _sql = new DoSQL(_config);
        }

        public List<StaffMember> staffMembers { get; set; }
        public List<Disease> diseases { get; set; }
        public List<Diagnosis> diagnoses { get; set; }
        public string staffCode { get; set; }
        public string clinCode { get; set; }
        public string staffName { get; set; }
        public StaffMember staffMember { get; set; }
        
        public string disCode;
        public DateTime startDate;
        public DateTime endDate;
        

        public bool isSuccess;
        public string Message;


        [Authorize]
        public async Task OnGet(string? clinicianCode, string? diseaseCode, DateTime? startDate, DateTime? endDate)
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
                    staffMember = await _staffData.GetStaffMemberDetails(User.Identity.Name);
                    //staffCode = _staffData.GetStaffCode(User.Identity.Name);
                    staffCode = staffMember.STAFF_CODE;
                    _sql.SqlWriteUsageAudit(staffCode, "", "Diagnosis Data index", _ip.GetIPAddress());
                }

                if(clinicianCode == null)
                {
                    clinicianCode = staffCode;
                }

                staffMembers = await _staffData.GetClinicalStaffList();
                var diseasesList = await _diseaseData.GetDiseaseList();
                diseases = diseasesList.OrderBy(d => d.DISEASE_CODE).ToList();

                clinCode = clinicianCode;
                disCode = diseaseCode;

                diagnoses = new List<Diagnosis>();

                if (diseaseCode != null)
                {
                    diagnoses = await _diseaseData.GetDiagnosisListByType(diseaseCode);                    
                }

                if (startDate == null)
                {
                    startDate = DateTime.Now.AddDays(-365);
                }
                if (endDate == null)
                {
                    endDate = DateTime.Now;
                }
                //Data
                
                //Numbers
                this.startDate = startDate.GetValueOrDefault();
                this.endDate = endDate.GetValueOrDefault();

                
            }
            catch (Exception ex)
            {
                Response.Redirect("Error?sError=" + ex.Message);
            }
        }
        public async Task OnPost(string? clinicianCode, string? diseaseCode, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                IPAddressFinder _ip = new IPAddressFinder(HttpContext);
                staffName = await _staffData.GetStaffName(User.Identity.Name);
                staffCode = await _staffData.GetStaffCode(User.Identity.Name);  
                staffMember = await _staffData.GetStaffMemberDetails(User.Identity.Name);
                _sql.SqlWriteUsageAudit(staffCode, $"Clinician={clinicianCode}", "Diagnosis Data index", _ip.GetIPAddress());
                staffMembers = await _staffData.GetStaffMemberList();
                var diseasesList = await _diseaseData.GetDiseaseList();
                diseases = diseasesList.OrderBy(d => d.DISEASE_CODE).ToList();

                if (clinicianCode != null)
                {
                    staffCode = clinicianCode;
                }

                diagnoses = new List<Diagnosis>();

                if (diseaseCode != null)
                {
                    diagnoses = await _diseaseData.GetDiagnosisListByType(diseaseCode);
                }

                clinCode = clinicianCode;
                
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
