using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sqy.beatsconnect.api.Models;
using sqy.beatsconnect.DataAccess;
using sqy.beatsconnect.DataEntities;
using sqy.beatsconnect.Helper;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Microsoft.Extensions.Primitives;
using sqy.beatsconnect.api.DTO;
using sqy.beatsconnect.api.Middleware;
using System.Web;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using MySql.Data.MySqlClient;

namespace sqy.beatsconnect.api.Controllers
{
    [BeatsAuthorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class LeadController : Controller
    {

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ILogger _logger;
        HttpClient client = new HttpClient();
        public LeadController(IHostingEnvironment hostingEnvironment, ILogger<LeadController> logger)
        {
            
            _hostingEnvironment = hostingEnvironment;
            _logger = logger;
        }

        private int currentUser
        {
            get
            {
                return Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            }
        }
        private ValidUserData currentUserData
        {
            get
            {
                return (ValidUserData) HttpContext.Items["CurrentUserData"];
            }
        }
        /// <summary>
        /// Gets a list of leads
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /GetMyLeads
        ///     {
        ///       "pageNo": 2,
        ///       "searchKey": "",
        ///       "developer": "All",
        ///       "assignedTo": -1,
        ///       "leadStatus": -1,
        ///       "pnLId": -1,
        ///       "project": "All",
        ///       "segmentID": -1,
        ///       "sharedWith": -1,
        ///       "source": "All",
        ///       "cpid": -1,
        ///       "dateType": 1,
        ///       "fromDate": "2016-11-01",
        ///       "toDate": "2017-04-01",
        ///       "t2oPnL": -1,
        ///       "notificationId": 0
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///     POST /GetMyLeads
        ///     {
        ///       "status": 1,
        ///       "message": "",
        ///       "data": {
        ///         "maxNumberOfPages": 0,
        ///         "currentPage": 1,
        ///         "filterInfo": "Warm 5-09-2017 To 5-12-2017",
        ///         "filterRes": [
        ///           {
        ///             "leadID": 2411218,
        ///             "project": "Other",
        ///             "name": "Manav Jain",
        ///             "phoneNumber": "9959022202",
        ///             "leadDate": "2017-09-08T10:08:13Z",
        ///             "assignedTo": "Sonali Gupta (SYME041)",
        ///             "status": "Warm",
        ///             "sharedWith": "",
        ///             "activityId": 8844137,
        ///             "feedbackStatus": false
        ///           },
        ///           {
        ///             "leadID": 2411216,
        ///             "project": "Other",
        ///             "name": "Niraj Chhajer",
        ///             "phoneNumber": "85291009325",
        ///             "leadDate": "2017-09-08T10:07:14Z",
        ///             "assignedTo": "Sonali Gupta (SYME041)",
        ///             "status": "Warm",
        ///             "sharedWith": "",
        ///             "activityId": 8844137,
        ///             "feedbackStatus": false
        ///           },
        ///           {
        ///             "leadID": 2411210,
        ///             "project": "Other",
        ///             "name": "Satyavrit Gaur",
        ///             "phoneNumber": "85298616729",
        ///             "leadDate": "2017-09-08T10:04:06Z",
        ///             "assignedTo": "Sonali Gupta (SYME041)",
        ///             "status": "Warm",
        ///             "sharedWith": "",
        ///             "activityId": 8844137,
        ///             "feedbackStatus": false
        ///           }
        ///         ]
        ///       }
        ///     }
        ///     
        /// </remarks>
        /// <returns>List of Leads</returns>
        [HttpPost]
        [Route("GetMyLeads")]
        public IActionResult GetMyLeads([FromBody] GetMyLeadsRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var defaultFromDate = DateTime.Now.AddMonths(-3).ToString("yyyy-MM-dd");
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetMyLeads,
                        PageNo = req.PageNo,
                        CurrentUser = currentUser,
                        SearchKey = "%" + req.SearchKey + "%",
                        Developer = req.Developer,
                        AssignedTo = req.AssignedTo == 0 ? currentUser : req.AssignedTo,
                        LeadStatus = req.LeadStatus,
                        PnlID = req.PnLId,
                        Project = req.Project,
                        SegmentID = req.SegmentID,
                        SharedTo = req.SharedWith,
                        Source = req.Source,
                        CPID = req.CPID,
                        DateType = req.DateType,
                        ToDate = DateTime.ParseExact(req.ToDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                        T2oPnL = req.T2oPnL,
                        NotificationId = req.NotificationId
                    };
                    if(String.IsNullOrEmpty(req.FromDate))
                    {
                        de.FromDate = DateTime.ParseExact(defaultFromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

                        de.HasFilters = 0;
                        /*
                        if(req.SearchKey != "%%")
                        {
                            de.HasFilters = 1;
                        }
                        */
                    }
                    else
                    {
                        de.FromDate = DateTime.ParseExact(req.FromDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        de.HasFilters = 1;
                    }
                    
                    var response = new GetMyLeadsResponseDTO();
                    var data = da.GetMyLeads(de);
                    var leads =
                        data[0].Select(
                            d => GenericHelper.CopyObject<DEBCApiLeadDBResponse, GetMyLeadsLeadResponseDTO>(d));
                    response.filterRes = leads.ToList();
                    response.MaxNumberOfPages = data[1][0].MaxNumberOfPages;
                    response.CurrentPage = data[1][0].CurrentPage;
                    response.FilterInfo = data[1][0].FilterInfo;

                    var status = response.filterRes.Count > 0 ? 1 : 1;
                    var message = response.filterRes.Count > 0 ? "" : "No record(s) found";
                    return Ok(new
                    {
                        Status = status,
                        Message = message,
                        Data = response
                    });
                    // return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message + "\n" + ex.StackTrace);
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Get Location By Type
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///     {
        ///         "locationType":0,
        ///         "locationId":0
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "data": [
        ///                     {
        ///                         "location_id": -1,
        ///                         "name": "--Select--",
        ///                         "parent_id": 0
        ///                     },
        ///                     {
        ///                         "location_id": 1,
        ///                         "name": "Aruba",
        ///                         "parent_id": 0
        ///                     }
        ///                   ]
        ///                 }
        ///         }
        ///     
        /// </remarks>
        /// <returns>List of Get Location By Type</returns>

        [HttpPost]
        [Route("GetLocationByType")]
        public IActionResult GetLocationByType([FromBody]GetLocationByTypeRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetLocationByType,
                        LocationType = req.LocationType,
                        ParentId = req.LocationId
                    };

                    var data = da.GetLocationByType(de);
                    var response = new
                    {
                        LocationDetail = data
                    };

                    return ApiHelper.CreateSuccessResponse(this, response, response.LocationDetail.Count > 0 ? "" : "No record(s) found", response.LocationDetail.Count > 0 ? 1 : 1);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Get Employee for sharedEmployee
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///     {
        ///      "key":""
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "empList": [
        ///                     {
        ///                         "employeeID": 8127,
        ///                         "employeeName": "Aakash Devaraju Nalla(SQC1186)"
        ///                     },
        ///                     {
        ///                         "employeeID": 220,
        ///                         "employeeName": "Aaron Lopez(SDC0190)"
        ///                     },
        ///                     {
        ///                         "employeeID": 8165,
        ///                         "employeeName": "Aarti Deepak Kadam(SQC1196)"
        ///                     }
        ///                 ]
        ///             
        ///     
        /// </remarks>
        /// <returns>List of Employee for sharedEmployee</returns>
        [HttpPost]
        [Route("GetSharedEmployeeList")]
        public IActionResult GetSharedEmployeeList([FromBody]GetEmpSharedEmployeeRequestDTO req)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetEmpForReassignment,
                        //CurrentUser=currentUser,
                        Key = "%" + req.key + "%"

                    };

                    var data = da.GetEmpForReassignment(de);
                    var response = new
                    {
                        empList = data
                    };

                    return ApiHelper.CreateSuccessResponse(this, response, response.empList.Count > 0 ? "" : "No record(s) found", response.empList.Count > 0 ? 1 : 1);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Get Lead Detail
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /Get Lead Detail
        ///     {
        ///		   "leadId":"3983710",
        ///		   "timeZone":"-06:00",
        ///		   "notificationId", 0
        ///		}
        ///		
        /// Sample Response:
        ///		
        ///     {
        ///         "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "timeZone": "+05:30",
        ///             "leadDetails": {
        ///                 "leadId": 3770656,
        ///                 "leadName": "Alok Kr",
        ///                 "leadDate": "2018-07-16T14:23:01Z",
        ///                 "project": "m3m",
        ///                 "source": "SELF GENERATED",
        ///                 "leadPhoneNum": "+919990286068",
        ///                 "leadAssignedTo": "Hitesh Singla (SDC0685)",
        ///                 "leadSharedWith": "",
        ///                 "leadEmailId": null,
        ///                 "leadStatus": "Hot",
        ///                 "updateEnabled": true,
        ///                 "cpId": 0,
        ///                 "cpCode": "",
        ///                 "cpDisplayName": "",
        ///                 "cpMobileNo": "",
        ///                 "leadHash": "642e8a0497f31db51a4ebdc322e166097206a6b25716206ebb24d4a251e41c70"
        ///             },
        ///             "details": {
        ///                 "budget": "",
        ///                 "budgetList": [
        ///                     {
        ///                         "ddlValue": 0,
        ///                         "ddlText": "-1"
        ///                     },
        ///                     {
        ///                         "ddlValue": 1,
        ///                         "ddlText": "0 - 20 Lac"
        ///                     },
        ///                     {
        ///                         "ddlValue": 2,
        ///                         "ddlText": "20 - 40 Lac"
        ///                     },
        ///                     {
        ///                         "ddlValue": 3,
        ///                         "ddlText": "40 - 60 Lac"
        ///                     },
        ///                     {
        ///                         "ddlValue": 4,
        ///                         "ddlText": "60 - 80 Lac"
        ///                     },
        ///                     {
        ///                         "ddlValue": 5,
        ///                         "ddlText": "80 Lac - 1 Cr"
        ///                     },
        ///                     {
        ///                         "ddlValue": 6,
        ///                         "ddlText": "1 - 1.5 Cr"
        ///                     },
        ///                     {
        ///                         "ddlValue": 7,
        ///                         "ddlText": "1.5 - 2 Cr"
        ///                     },
        ///                     {
        ///                         "ddlValue": 8,
        ///                         "ddlText": "2 - 3 Cr"
        ///                     },
        ///                     {
        ///                         "ddlValue": 9,
        ///                         "ddlText": "> 3 Cr"
        ///                     }
        ///                 ],
        ///                 "developer": "Other",
        ///                 "project": "m3m",
        ///                 "city": null,
        ///                 "segment": "Primary"
        ///             },
        ///             "updateActivity": {
        ///                 "status": "Hot",
        ///                 "reason": [
        ///                     {
        ///                         "refType": "Return_Reason",
        ///                         "refID": 1,
        ///                         "refName": "Irrelevant Client Location",
        ///                         "refParentId": 0
        ///                     },
        ///                     {
        ///                         "refType": "Return_Reason",
        ///                         "refID": 2,
        ///                         "refName": "Client Not Interested",
        ///                         "refParentId": 0
        ///                     }
        ///                 ],
        ///                 "statusList": [
        ///                     {
        ///                         "statusId": "2",
        ///                         "statusName": "Hot"
        ///                     },
        ///                     {
        ///                         "statusId": "3",
        ///                         "statusName": "Warm"
        ///                     },
        ///                     {
        ///                         "statusId": "4",
        ///                         "statusName": "Cold"
        ///                     },
        ///                     {
        ///                         "statusId": "6",
        ///                         "statusName": "Not-Interested"
        ///                     }
        ///                     {
        ///                         "statusId": "9",
        ///                         "statusName": "Unshare"
        ///                     }
        ///                 ],
        ///                 "activityList": [
        ///                     {
        ///                         "activityId": "1",
        ///                         "activityName": "F2F",
        ///                         "maxProjects": 3,
        ///                         "projectsRequired": true
        ///                     },
        ///                     {
        ///                         "activityId": "2",
        ///                         "activityName": "Calls",
        ///                         "maxProjects": 3,
        ///                         "projectsRequired": false
        ///                     },
        ///                     {
        ///                         "activityId": "3",
        ///                         "activityName": "Site Visit",
        ///                         "maxProjects": 1,
        ///                         "projectsRequired": true
        ///                     },
        ///                     {
        ///                         "activityId": "55",
        ///                         "activityName": "Closure Meeting",
        ///                         "maxProjects": 1,
        ///                         "projectsRequired": true
        ///                     }
        ///                 ],
        ///                 "activity": "Calls",
        ///                 "activityProjects": [
        ///                     {
        ///                         "productId": 1,
        ///                         "displayName": "Unitech - Unitech Fresco",
        ///                         "otherStr": null
        ///                     },
        ///                     {
        ///                         "productId": -1,
        ///                         "displayName": "Other",
        ///                         "otherStr": "Dummy Project"
        ///                     },
        ///                     {
        ///                         "productId": 3,
        ///                         "displayName": "Bestech - Bestech Park View Spa Next",
        ///                         "otherStr": null
        ///                     }
        ///                 ]
        ///             },
        ///             "clientInteraction": [
        ///                 {
        ///                     "interactionDate": "2018-08-09T11:24:58Z",
        ///                     "nextInteractionDate": "2018-08-15T11:33:12Z",
        ///                     "comments": "testing comments updation",
        ///                     "statusDesc": "Hot",
        ///                     "nextActivity": "Calls",
        ///                     "lastActivity": "Calls",
        ///                     "updatedBy": "Hitesh Singla (SDC0685)",
        ///                     "activityId": 17110111,
        ///                     "feedbackStatus": true,
        ///                     "activityUpdateAllowed": false,
        ///                     "products": []
        ///                 },
        ///                 {
        ///                     "interactionDate": "2018-07-16T14:23:01Z",
        ///                     "nextInteractionDate": null,
        ///                     "comments": "Test test today and test tomorrow morning test",
        ///                     "statusDesc": "New",
        ///                     "nextActivity": "",
        ///                     "lastActivity": "",
        ///                     "updatedBy": "Hitesh Singla (SDC0685)",
        ///                     "activityId": 16032712,
        ///                     "feedbackStatus": true,
        ///                     "activityUpdateAllowed": false,
        ///                     "products": []
        ///                 }
        ///             ],
        ///             "basicInfo": {
        ///                 "leadName": "Alok Kr",
        ///                 "salutationList": "| Dr.| Brig.| Capt.| Col.| Mr.| Mrs.| Ms.| Prof.",
        ///                 "website": "",
        ///                 "cityId": 0,
        ///                 "stateId": 0,
        ///                 "countryId": 0,
        ///                 "address": "",
        ///                 "cityName": null,
        ///                 "companyName": "",
        ///                 "title": "",
        ///                 "zipCode": "",
        ///                 "industry": "",
        ///                 "salutationValue": null
        ///             }
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>List of GetLead ActivityDetail</returns>
        [HttpPost]
        [Route("GetLeadDetail")]
        public IActionResult GetLeadDetail([FromBody]GetLeadActivityRequestDTO req)
        {
            try
            {
                double timeDiff = TimeZoneHelper.getTimeZone(req.TimeZone);
                if (ModelState.IsValid)
                {
                    if (req.NotificationId != 0)
                    {
                        var leadId = BeatsAPIHelper.ReadNotification(req.NotificationId, currentUserData.ApiAccessToken);
                        if (req.LeadId == 0)
                        {
                            req.LeadId = Convert.ToInt32(leadId);
                        }
                    }
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetLeadActivity,
                        LeadId = req.LeadId,
                        CurrentUser = currentUser,
                        UtcTimeDiff = Convert.ToInt32(timeDiff),
                        NotificationId = req.NotificationId
                    };
                    var data = da.GetLeadActivity(de)[0];
                    #region Budget

                    var leadID = data[0].LeadID;
                    DEBCApiLead BudgetList = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetBudgetList,
                        LeadId = leadID
                    };
                    var data1 = da.GetList<GetBudgetList>(BudgetList);
                    #endregion

                    #region Reason

                    DEBCApiLead ReasonList = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetReasonList,
                    };
                    var data2 = da.GetList<GetResionList>(ReasonList);
                    #endregion

                    #region Activity

                    DEBCApiLead ActivityList = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetActivityList,
                        LeadId = leadID
                    };
                    var data3 = da.GetList<GetActivityList>(ActivityList);
                    #endregion

                    #region ClentInteraction
                    de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetClientInteraction,
                        LeadId = leadID,
                        CurrentUser = currentUser
                    };
                    // var data4 = da.GetList<GetClientInteraction>(de);
                    var data4 = da.GetLists<BCApiLeadDBResponse>(de);

                    var clientInteractions =
                       data4[0].Select(d => GenericHelper.CopyObject<BCApiLeadDBResponse, ClientInteractionDTO>(d)).ToList();

                    var products = data4[1];
                    
                    foreach(var clientInteraction in clientInteractions)
                    {
                        clientInteraction.Products = products
                            .Where(p => p.ActivityId == clientInteraction.ActivityId)
                            .Select(p => new ProductDTO()
                            {
                                ProductId = p.ProductId,
                                DisplayName = p.DisplayName,
                                OtherStr = p.OtherStr
                            }).ToList();
                    }
                    #endregion


                    #region getactivityprojects
                    de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetActivityProjects,
                        LeadId = leadID
                    };

                    var apData = da.GetList<BCApiLeadDBResponse>(de);
                    var activityProjects =apData.Select(d => GenericHelper.CopyObject<BCApiLeadDBResponse, ActivityProjectDTO>(d)).ToList();
                    #endregion
                    #region StatusList

                    List<StatusListDTO> StatusList = new List<StatusListDTO>();
                    var chkDate = DateTime.Now;
                    if (data[0].LeadReassignmentDate != null)
                    {
                        if (data[0].SegmentID != 9)
                        {
                            var dateGenCheck = Convert.ToDateTime(data[0].LeadReassignmentDate).AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                //StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                //if (data[0].SharedWith == currentUser)
                                //    StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                //StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                        else
                        {
                            var dateGenCheck = Convert.ToDateTime(data[0].LeadReassignmentDate).AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                               
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }

                    }
                    else
                    {
                        if (data[0].SegmentID != 9)
                        {
                            var dateGenCheck = data[0].LeadGenerationDate.AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                               
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                        else
                        {
                            var dateGenCheck = data[0].LeadGenerationDate.AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });

                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data[0].AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });

                                if (data[0].SharedWith == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                    }
                    #endregion

                    #region Response

                    var response = new
                    {
                        TimeZone = req.TimeZone,
                        LeadDetails = new
                        {
                            LeadId = data[0].LeadID,
                            leadName = data[0].LeadName,
                            LeadDate = data[0].Leaddate,
                            project = data[0].Project,
                            source = data[0].Source,
                            leadPhoneNum = data[0].LeadPhoneNum,
                            leadAssignedTo = data[0].LeadAssignedTo,
                            leadSharedWith = data[0].LeadSharedWith,
                            leadEmailId = data[0].LeadEmailId,
                            leadStatus = data[0].LeadStatus,
                            updateEnabled = data[0].updateEnabled,
                            CpId = data[0].CPID,
                            CpCode = data[0].CPCode,
                            CpDisplayName = data[0].CPDisplayName,
                            CpMobileNo = data[0].CPMobileNo,
                            leadHash = data[0].LeadHash == null ? "" : data[0].LeadHash,
                            requestTestimonialCnt = data[0].requestTestimonialCnt,
                            customerTestimonialCnt = data[0].customerTestimonialCnt,
                            IsEligibleTestimonial = data[0].IsEligibleTestimonial
                        },
                        Details = new
                        {
                            Budget = data[0].BudgetDesc,
                            budgetList = data1,
                            Developer = data[0].Developer,
                            project = data[0].Project,
                            city = data[0].City,
                            Segment = data[0].Segment
                        },
                        UpdateActivity = new
                        {
                            status = data[0].LeadStatus,
                            reason = data2,
                            statusList = StatusList,
                            activityList = data3,
                            activity = data[0].LeadActivity,
                            ActivityProjects = activityProjects,
                        },
                        ClientInteraction = clientInteractions,
                        BasicInfo = new
                        {
                            leadName = data[0].LeadName,
                            salutationList = "| Dr.| Brig.| Capt.| Col.| Mr.| Mrs.| Ms.| Prof.",
                            //midName = data.MiddleName,
                            //lastName = data.LastName,
                            website = data[0].Website,
                            cityId = data[0].CityID,
                            stateId = data[0].StateID,
                            countryId = data[0].CountryID,
                            address = data[0].Address,
                            CityName = data[0].City,
                            CompanyName = data[0].CompanyName,
                            Title = data[0].Title,
                            zipCode = data[0].ZipCode,
                            industry = data[0].Industry,
                            salutationValue = data[0].Salutation
                        }

                    };
                    #endregion
                    return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Gets Lead Activity Detail
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /Get Lead Activity Detail
        ///     {
        ///		   "leadId":"1067437"
        ///		}
        ///		
        /// Sample Response:
        /// 
        ///     {
        ///         "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "updateActivity": {
        ///                 "reason": [
        ///                     {
        ///                         "refType": "Return_Reason",
        ///                         "refID": 1,
        ///                         "refName": "Irrelevant Client Location",
        ///                         "refParentId": 0
        ///                     },
        ///                     {
        ///                         "refType": "Return_Reason",
        ///                         "refID": 2,
        ///                         "refName": "Client Not Interested",
        ///                         "refParentId": 0
        ///                     },
        ///                     {
        ///                         "refType": "Return_Reason",
        ///                         "refID": 4,
        ///                         "refName": "Out Of Budget",
        ///                         "refParentId": 0
        ///                     }
        ///                 ],
        ///                 "status": "Warm",
        ///                 "activityList": [
        ///                     {
        ///                         "activityId": "23",
        ///                         "activityName": "F2F",
        ///                         "maxProjects": 3,
        ///                         "projectsRequired": true
        ///                     },
        ///                     {
        ///                         "activityId": "25",
        ///                         "activityName": "Calls",
        ///                         "maxProjects": 3,
        ///                         "projectsRequired": false
        ///                     },
        ///                     {
        ///                         "activityId": "27",
        ///                         "activityName": "Site Visit",
        ///                         "maxProjects": 1,
        ///                         "projectsRequired": true
        ///                     },
        ///                     {
        ///                         "activityId": "56",
        ///                         "activityName": "Closure Meeting",
        ///                         "maxProjects": 1,
        ///                         "projectsRequired": true
        ///                     }
        ///                 ],
        ///                 "statusList": [
        ///                     {
        ///                         "statusId": "2",
        ///                         "statusName": "Hot"
        ///                     },
        ///                     {
        ///                         "statusId": "3",
        ///                         "statusName": "Warm"
        ///                     },
        ///                     {
        ///                         "statusId": "4",
        ///                         "statusName": "Cold"
        ///                     },
        ///                     {
        ///                         "statusId": "6",
        ///                         "statusName": "Not-Interested"
        ///                     }
        ///                 ],
        ///                 "activity": "F2F",
        ///                 "activityProjects": [
        ///                     {
        ///                         "productId": -1,
        ///                         "displayName": "Other",
        ///                         "otherStr": "Azadi - Emaar Beachfront"
        ///                     }
        ///                 ]
        ///             }
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>List of Leads</returns>
        [HttpPost]
        [Route("GetActivityDetail")]
        public IActionResult GetActivityDetail([FromBody]GetLeadActivityRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetLeadReturnActivity,
                        LeadId = req.LeadId,
                        CurrentUser = currentUser,

                    };

                    var data = da.GetLeadReturnActivity(de)[0];
                    DEBCApiLead ReasonList = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetReasonList,
                    };
                    var Reason = da.GetList<GetResionList>(ReasonList);

                    DEBCApiLead ActivityList = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetActivityList,
                        LeadId = req.LeadId
                    };
                    var Activity = da.GetList<GetActivityList>(ActivityList);
                    
                    de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetActivityProjects,
                        LeadId = req.LeadId
                    };

                    var apData = da.GetList<BCApiLeadDBResponse>(de);
                    var activityProjects = apData.Select(d => GenericHelper.CopyObject<BCApiLeadDBResponse, ActivityProjectDTO>(d)).ToList();

                    List<StatusListDTO> StatusList = new List<StatusListDTO>();

                    var chkDate = DateTime.Now;
                    if (data.LeadReassignmentDate != null)
                    {
                        if (data.SegmentID != 9)
                        {
                            var dateGenCheck = Convert.ToDateTime(data.LeadReassignmentDate).AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                             
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                               
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                        else
                        {
                            var dateGenCheck = Convert.ToDateTime(data.LeadReassignmentDate).AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                              
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }

                    }
                    else
                    {
                        if (data.SegmentID != 9)
                        {
                            var dateGenCheck = data.LeadGenerationDate.AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                        else
                        {
                            var dateGenCheck = data.LeadGenerationDate.AddDays(3);
                            if (chkDate.CompareTo(dateGenCheck) >= 0)
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "6", statusName = "Not-Interested" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                            else
                            {
                                StatusList.Add(new StatusListDTO() { statusId = "2", statusName = "Hot" });
                                StatusList.Add(new StatusListDTO() { statusId = "3", statusName = "Warm" });
                                StatusList.Add(new StatusListDTO() { statusId = "4", statusName = "Cold" });
                                if (data.AssignedTo == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "7", statusName = "Return" });
                                if (data.SharedWithId == currentUser)
                                    StatusList.Add(new StatusListDTO() { statusId = "9", statusName = "Unshare" });
                            }
                        }
                    }

                    var response = new
                    {
                        updateActivity = new
                        {
                            //budget = data.BudgetDesc,
                            //project = data.Project,
                            //budgetList = Budget,
                            reason = Reason,
                            status = data.StatusDesc,
                            //developer = data.Developer,
                            activityList = Activity,
                            statusList = StatusList,
                            activity = data.Activity,
                            ActivityProjects = activityProjects
                        },
                    };

                    return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }



        /// <summary>
        /// Gets a list of Filter
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /getFilters
        ///     {
        ///		  
        ///		}
        ///		
        /// Sample Response:
        /// 
        ///     POST /getFilters
        ///     {
        ///        "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "dateRange": null,
        ///             "segment": null,
        ///             "leadSource": null,
        ///             "leadStatus": null,
        ///             "source": [
        ///                 {
        ///                     "sourceName": "All"
        ///                 },
        ///                 {
        ///                     "sourceName": ""
        ///                 },
        ///                 {
        ///                     "sourceName": "A99 Acres_Webportal Listing_All-12"
        ///                 },
        ///                 {
        ///                     "sourceName": "Advertisement"
        ///                 },
        ///             ]
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>List of Leads</returns>
        [HttpGet, Route("GetFilters")]
        public IActionResult GetFilters()
        {

            var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetAllSource,
                        CurrentUser = currentUser,
                    };
                    var obj = new GetFiltersResponseDTO();
                    var data = da.GetList<SourceDTO>(de);
                    obj.source = data;

                    //DEBCApiLead de1 = new DEBCApiLead()
                    //{
                    //    CallValue = DEBCApiLeadCallValues.GetAllDeveloper,
                    //    CurrentUser = currentUser,
                    //};
                    //var data1 = da.GetList<DeveloperDTO>(de1);
                    //obj.developer = data1;

                    //DEBCApiLead de2 = new DEBCApiLead()
                    //{
                    //    CallValue = DEBCApiLeadCallValues.GetAllProject,
                    //    CurrentUser = currentUser,
                    //};
                    //var data2 = da.GetList<ProjectDTO>(de2);
                    //obj.project = data2;

                    DEBCApiLead de3 = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetAssignedToEmployee,
                        CurrentUser = currentUser,
                    };
                    var data3 = da.GetList<EmpListDTO>(de3);
                    obj.empList = data3;

                    DEBCApiLead de4 = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetAllPNL,
                        CurrentUser = currentUser,
                    };
                    var data4 = da.GetList<PnlListDTO>(de4);
                    obj.pnlList = data4;

                    DEBCApiLead de5 = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetSharedWithEmployee,
                        CurrentUser = currentUser,
                    };
                    var data5 = da.GetList<sharedToDTO>(de5);
                    obj.sharedTo = data5;

                    // Create a list of DateRange.
                    List<DateRangeDTO> daterange = new List<DateRangeDTO>();

                    // Add DateRange to the list.
                    daterange.Add(new DateRangeDTO() { dateRangeId = 3, dateRangeValue = "Next Interaction Date" });
                    daterange.Add(new DateRangeDTO() { dateRangeId = 1, dateRangeValue = "Lead Generation Date" });
                    daterange.Add(new DateRangeDTO() { dateRangeId = 2, dateRangeValue = "Last Interaction Date" });
                    obj.dateRange = daterange;

                    // Create a list of segment.
                    List<SegmentDTO> segment = new List<SegmentDTO>();

                    // Add segment to the list.
                    segment.Add(new SegmentDTO() { segmentId = -1, segmentName = "All" });
                    segment.Add(new SegmentDTO() { segmentId = 7, segmentName = "Primary" });
                    segment.Add(new SegmentDTO() { segmentId = 9, segmentName = "Capital" });
                    segment.Add(new SegmentDTO() { segmentId = 13, segmentName = "Connect" });
                    segment.Add(new SegmentDTO() { segmentId = 15, segmentName = "Lease" });
                    segment.Add(new SegmentDTO() { segmentId = 16, segmentName = "Resale" });
                    segment.Add(new SegmentDTO() { segmentId = 17, segmentName = "Builder Loan" });

                    obj.segment = segment;

                    // Create a list of leadSource.
                    List<LeadStatusDTO> leadStatus = new List<LeadStatusDTO>();

                    // Add leadSource to the list.
                    leadStatus.Add(new LeadStatusDTO() { statusId = -1, statusName = "All" });
                    leadStatus.Add(new LeadStatusDTO() { statusId = 2, statusName = "Hot" });
                    leadStatus.Add(new LeadStatusDTO() { statusId = 3, statusName = "Warm" });
                    leadStatus.Add(new LeadStatusDTO() { statusId = 4, statusName = "Cold" });
                    leadStatus.Add(new LeadStatusDTO() { statusId = 5, statusName = "Win" });
                    leadStatus.Add(new LeadStatusDTO() { statusId = 1, statusName = "New" });
                    obj.leadStatus = leadStatus;



                    return ApiHelper.CreateSuccessResponse(this, obj);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Update Activity against a lead
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     {
        ///             "callActivityId": 522607,
        ///             "comments": "test",
        ///             "currentActivityID": "2",
        ///             "latitude": 0.0,
        ///             "leadID": 3770656,
        ///             "leadStatusID": "2",
        ///             "longitude": 0.0,
        ///             "nextActivityID": "2",
        ///             "nextInteractionDate": "2018-08-15 11:33:12",
        ///             "reason": "0",
        ///             "subReason": "0",
        ///             "leadActivityId": 0,
        ///             "eventDetailId": 0,
        ///             "products": [{
        ///                     "productId": 1,
        ///                     "displayName": "Unitech Fresco",
        ///                     "otherStr": ""
        ///             }, {
        ///                     "productId": -1,
        ///                     "displayName": "Other",
        ///                     "otherStr": "Dummy Project"
        ///             }]
        ///     }
        ///		
        /// Sample Response:
        ///
        ///     {
        ///         "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "message": "Updated Successfully"
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>Success Message</returns>
        [HttpPost]
        [Route("UpdateActivity")]
        public IActionResult UpdateActivity([FromBody] UpdateActivityRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.UpdateActivity,
                        CurrentActivityID = req.CurrentActivityID,
                        //InteractionDate=DateTime.Now,
                        Latitude = req.Latitude,
                        LeadStatusID = req.LeadStatusID,
                        Longitude = req.Longitude,
                        NextActivityID = req.NextActivityID,
                        //Project = req.Project,
                        Reason = req.Reason,
                        SubReason = req.SubReason,
                        Comments = req.Comments,
                        CallActivityID = req.CallActivityID,
                        //BudgetID = req.BudgetID,
                        CurrentUser = currentUser,
                        LeadId = req.LeadID,
                        LeadActivityId = req.LeadActivityId
                    };
                    if (req.LeadStatusID != 6 && req.LeadStatusID != 7)
                    {
                        if (req.NextInteractionDate != null)
                            de.NextInteractionDate = DateTime.ParseExact(req.NextInteractionDate, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                    //var data = da.GetList<DEBCApiLeadDBResponse>(de);
                    var data = da.GetMyLeads(de);
                    var response = new {
                        LeadActivityId = data[0][0].activityId,
                    };


                    /**** add the projects if exists ****/
                    if (req.Products != null)
                    {
                        de = new DEBCApiLead()
                        {
                            CallValue = DEBCApiLeadCallValues.AddProjectForLeadActivity
                        };
                        foreach(var product in req.Products)
                        {
                            de.LeadActivityId = response.LeadActivityId;
                            de.ProductId = product.ProductId;
                            de.ProductName = product.ProductId == -1 ? product.OtherStr : "";

                            da.GetList<BCApiLeadDBResponse>(de);
                        }
                    }

                    /**** create rsvp ****/
                    var message = "Successfully Updated";
                    if (req.EventDetailId != 0)
                    {
                        try
                        {
                            // get lead information
                            de = new DEBCApiLead
                            {
                                CallValue = DEBCApiLeadCallValues.GetLeadInformation,
                                LeadId = req.LeadID,
                                CurrentUser = currentUser
                            };
                            var leadData = da.GetList<BCApiLeadDBResponse>(de);
                            // create rsvp
                            if (leadData.Count > 0)
                            {
                                var leadInfo = leadData[0];
                                DABCApiResponse daResponse = new DABCApiResponse();
                                DEBCApiResponse deResponse = new DEBCApiResponse()
                                {
                                    CallValue = DEBCApiResponseCallValues.CreateRSVP,
                                    ResponseId = req.CallActivityID,
                                    EmployeeID = leadInfo.EmployeeCode,
                                    PhoneNumber = leadInfo.PhoneNumber,
                                    CountryCode = leadInfo.CountryCode,
                                    // CallDuration = req.CallDuration,
                                    // RingDuration = req.RingDuration,
                                    // DispositionStatus = Enumeration.GetAll<DEDespositionType>().Where(x => x.Value == req.DispositionStatus.ToString().Replace(' ', '_')).SingleOrDefault().Key,
                                    CustomerName = leadInfo.CustomerName,
                                    // Project = req.Project,
                                    // Comments = leadInfo.Comments,
                                    EventDetailId = req.EventDetailId,
                                    Email = leadInfo.Email,
                                    City = leadInfo.City,
                                    Comments = req.Comments
                                };
                                daResponse.CreateRSVP(deResponse);
                            }
                            else
                            {
                                throw new ApplicationException("Lead infomation not found");
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.LogInformation("RSVP Creation Failure RecNo: " + response.LeadActivityId + " EventDetailId: " + req.EventDetailId);
                            message += " , Fail to create RSVP: " + ex.Message;
                        }
                    }
                    _logger.LogInformation("Debugging OTP RecNo: " + response.LeadActivityId);
                    
                    if(req.LeadActivityId == 0 && data[1].Count > 0 && data[1][0].updateEnabled == true)
                    {
                        _logger.LogInformation("Debugging OTP RecNo: " + response.LeadActivityId + " UpdateEnabled: " + data[1][0].updateEnabled);
                        CreateOtp(data[0][0].activityId, 1);
                    }

                    if (data.Count > 2 && data[2][0].LeadID.ToString() != "0")
                    {
                        try
                        {
                            var sharedLeadEmailDetails = AppSettingsConf.EMailTemplatePath(MailDetails.LeadReturnVerification);
                            var sharedLeadTemplate = _hostingEnvironment.ContentRootPath + sharedLeadEmailDetails.TemplateUrl;
                            // var sharedLeadTemplate = _hostingEnvironment.ContentRootPath + @"\View\LeadShared.html";
                            Console.WriteLine(sharedLeadTemplate);
                            string mailBody = string.Empty;
                            using (StreamReader reader = new StreamReader(sharedLeadTemplate))
                            {
                                mailBody = reader.ReadToEnd();
                            }
                            var row = data[0][0];

                            var mailSubject = sharedLeadEmailDetails.Subject
                                .Replace("{LeadID}", row.LeadID.ToString())
                                .Replace("{LeadStatus}", row.LeadStatusText);
                            mailBody = mailBody
                                .Replace("{MailSubject}", mailSubject)
                                .Replace("{PHEmployeeName}", row.PHEmployeeName)
                                .Replace("{LeadStatus}", row.LeadStatusText)
                                .Replace("{EmployeeName}", row.EmployeeName)
                                .Replace("{LeadID}", row.LeadID.ToString())
                                .Replace("{ClientName}", row.ClientName)
                                .Replace("{ClientNo}", row.ClientNo)
                                .Replace("{ClientEmail}", row.ClientEmail)
                                .Replace("{ProjectName}", row.ProjectName);

                            var mailTo = Convert.ToString(row.ToEmails);
                            var mailCc = Convert.ToString(row.CCEmails);
                            var mailBcc = Convert.ToString(row.BCCEmails);

                            SendMail.f_sendMailFromCRM(mailTo, mailCc, mailBcc, "", mailBody, mailSubject, null);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }

                    return ApiHelper.CreateSuccessResponse(this, response, message, 1);

                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Get Team For PnlId
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///     {
        ///      "pnlID":""
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": [
        ///                 {
        ///                     "empId": -1,
        ///                     "empName": "All"
        ///                 },
        ///                 {
        ///                     "empId": 29,
        ///                     "empName": "Sonali Gupta"
        ///                 },
        ///                 {
        ///                     "empId": 489,
        ///                     "empName": "Sucheta Banik"
        ///                 },
        ///                 {
        ///                     "empId": 9203,
        ///                     "empName": "Mohit Yadav"
        ///                 }
        ///             ]
        ///         }
        ///     
        /// </remarks>
        /// <returns>List of Team for pnlid</returns>
        [HttpPost]
        [Route("GetTeamForPnlId")]
        public IActionResult GetTeamForPnlId([FromBody]GetTeamForPnlIdRequestDTO req)
        {

            var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {

                        CallValue = DEBCApiLeadCallValues.GetTeamForPnlId,
                        PnlID = req.PnlID,
                        CurrentUser = currentUser
                    };
                    var data = da.GetList<GetTeamForPnlIdResponseDTO>(de);
                    var response =
                    new
                    {
                        pnlTeams = data
                    };
                    return ApiHelper.CreateSuccessResponse(this, response, response.pnlTeams.Count > 0 ? "" : "No record(s) found", response.pnlTeams.Count > 0 ? 1 : 1);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Get My Team 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///     {
        ///      "empId":""
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "myTeamList": [
        ///                     {
        ///                         "employeeName": "Sonali Gupta",
        ///                         "employeeId": 29,
        ///                         "userName": "SYME041",
        ///                         "level": "T1",
        ///                         "pnlName": "HK IPM",
        ///                         "leadAllowed": true,
        ///                         "hasTeam": true,
        ///                         "leadStatus": true
        ///                     }
        ///                 ]
        ///             }
        ///         }
        ///     
        /// </remarks>
        /// <returns>List of My Team </returns>
        [HttpPost]
        [Route("GetMyTeam")]
        public IActionResult GetMyTeam([FromBody]GetMyTeamRequestDTO req)
        {

            var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {

                        CallValue = DEBCApiLeadCallValues.GetMyTeam,
                        CurrentUser = currentUser,
                        EmpId = req.empId
                    };
                    var data = da.GetList<GetMyTeamResponseDTO>(de);
                    var response = new
                    {
                        myTeamList = data
                    };
                    return ApiHelper.CreateSuccessResponse(this, response, response.myTeamList.Count > 0 ? "" : "No record(s) found", response.myTeamList.Count > 0 ? 1 : 1);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Lead Share to Employee 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///         {
        ///         "comments":"Testing Testing Testing Testing Testing Testing Testing Testing Testing Testing ",
        ///         "empId":5555,
        ///         "leadId":457681
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "message": "Lead share to successfully."
        ///             }
        ///         }
        ///     
        /// </remarks>
        /// <returns>LeadShare To Employee</returns>
        [HttpPost]
        [Route("LeadShareToEmployee")]
        public IActionResult LeadShareToEmployee([FromBody] LeadShareToEmployeeRequestDTO req)
        {

            var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {

                        CallValue = DEBCApiLeadCallValues.LeadShareToEmployee,
                        CurrentUser = currentUser,
                        SharedTo = req.EmpId,
                        SharedComments = req.Comments,
                        LeadId = req.LeadId

                    };
                    var data = da.GetList<DELeadShareToEmployeeResponseDTO>(de);
                    if (data.Count > 0)
                    {
                        if (data[0].Message == "")
                        {
                            var _path = AppSettingsConf.EMailTemplatePath(MailDetails.ShareLead);
                            var sharedLeadTemplate = _hostingEnvironment.ContentRootPath + _path.TemplateUrl;
                            // var sharedLeadTemplate = _hostingEnvironment.ContentRootPath + @"\View\LeadShared.html";
                            Console.WriteLine(sharedLeadTemplate);
                            string mailBody = string.Empty;
                            using (StreamReader reader = new StreamReader(sharedLeadTemplate))
                            {
                                mailBody = reader.ReadToEnd();

                            }
                            var row = data;

                            var sendEmail = Convert.ToString(data[0].SendEmail);
                            mailBody = mailBody
                                .Replace("{SharedWithName}", Convert.ToString(data[0].SharedWithName))
                                .Replace("{SharedByName}", Convert.ToString(data[0].SharedByName))
                                .Replace("{LeadID}", Convert.ToString(data[0].LeadID))
                                .Replace("{SharedComments}", Convert.ToString(data[0].SharedComments));

                            var mailTo = Convert.ToString(data[0].MailTo);
                            var mailCc = Convert.ToString(data[0].MailCc);
                            var mailBcc = Convert.ToString(data[0].MailBcc);
                            var mailSubject = Convert.ToString(data[0].MailSubject);

                            if (sendEmail == "1")
                            {
                                SendMail.f_sendMailFromCRM(mailTo, mailCc, mailBcc, "", mailBody, mailSubject, null);
                                return ApiHelper.UpdateSuccessResponse(this, "Lead share to successfully.");
                            }
                            else
                                return ApiHelper.CreateErrorResponse(this, "Lead Share Mail Not Successfully");
                        }
                        else
                        {
                            return ApiHelper.UpdateSuccessResponse(this, data[0].Message);
                        }

                    }
                    else
                    {
                        return ApiHelper.CreateSuccessResponse(this, data);
                    }

                }
                else
                {
                    var modelError = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0];
                    var error = modelError.ErrorMessage == "" ? modelError.Exception.Message : modelError.ErrorMessage;

                    return ApiHelper.CreateErrorResponse(this, error);
                    // throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Get Interaction Details
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///         {
        ///         "date":"2017-08-01T05:06:58Z",
        ///         "timeZone":"-06:00",
        ///         "PageNo":"1",
        ///         "interactiontype":"1"
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///		{
        ///		    "status": 1,
        ///		    "message": "",
        ///		    "data": {
        ///		        "maxNumberOfPages": 1,
        ///		        "currentPage": 1,
        ///		        "timeZone": "-06:00",
        ///		        "interactionDetails": [
        ///		            {
        ///		                "leadID": 2432526,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+919024569754",
        ///		                "firstName": "test New Lead 2",
        ///		                "nextInteractionDate": "2018-01-13T18:33:25Z",
        ///		                "nextActivity": "F2F",
        ///		                "date": "13",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 52321,
        ///		                "leadStatus": "Hot",
        ///		                "phoneNumber": "+911234567890",
        ///		                "firstName": "test",
        ///		                "nextInteractionDate": "2017-09-28T16:57:32Z",
        ///		                "nextActivity": "Calls",
        ///		                "date": "28",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 2371426,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+919687712385",
        ///		                "firstName": "cgcuc",
        ///		                "nextInteractionDate": "2017-09-23T11:26:01Z",
        ///		                "nextActivity": "F2F",
        ///		                "date": "23",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 2372678,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+919647896325",
        ///		                "firstName": "John",
        ///		                "nextInteractionDate": "2017-09-18T16:40:01Z",
        ///		                "nextActivity": "F2F",
        ///		                "date": "18",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 1421562,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+918545214587",
        ///		                "firstName": "Udit",
        ///		                "nextInteractionDate": "2017-08-27T11:30:37Z",
        ///		                "nextActivity": "Calls",
        ///		                "date": "27",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 1504758,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+919875465852",
        ///		                "firstName": "test lead",
        ///		                "nextInteractionDate": "2017-08-17T13:03:26Z",
        ///		                "nextActivity": "Calls",
        ///		                "date": "17",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 460749,
        ///		                "leadStatus": "Hot",
        ///		                "phoneNumber": "+919823333856",
        ///		                "firstName": "Owais randera",
        ///		                "nextInteractionDate": "2017-07-19T15:28:01Z",
        ///		                "nextActivity": "Site Visit",
        ///		                "date": "19",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 1408908,
        ///		                "leadStatus": "Hot",
        ///		                "phoneNumber": "+919582122549",
        ///		                "firstName": "chirag",
        ///		                "nextInteractionDate": "2017-05-16T15:00:00Z",
        ///		                "nextActivity": "F2F",
        ///		                "date": "16",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 1420530,
        ///		                "leadStatus": "Warm",
        ///		                "phoneNumber": "+918860853232",
        ///		                "firstName": "user name",
        ///		                "nextInteractionDate": "2017-05-06T11:45:00Z",
        ///		                "nextActivity": "F2F",
        ///		                "date": "6",
        ///		                "isPlanned": false
        ///		            },
        ///		            {
        ///		                "leadID": 751879,
        ///		                "leadStatus": "Hot",
        ///		                "phoneNumber": "+918860596130",
        ///		                "firstName": "Suresh Tiwari",
        ///		                "nextInteractionDate": "2017-03-09T11:04:36Z",
        ///		                "nextActivity": "Calls",
        ///		                "date": "9",
        ///		                "isPlanned": false
        ///		            }
        ///		        ]
        ///		    }
        ///		}
        ///     
        /// </remarks>
        /// <returns>List of interactiondetails </returns>
        [HttpPost]
        [Route("GetInteractionDetails")]
        public IActionResult GetInteractionDetails([FromBody]DEGetInteractionDetailsRequestDTO req)
        {
            try
            {
                double timeDiff = TimeZoneHelper.getTimeZone(req.TimeZone);
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {

                        CallValue = DEBCApiLeadCallValues.GetInteractionDetails,
                        CurrentUser = currentUser,
                        InteractionType = req.InteractionType,
                        FromDate = req.date,
                        UtcTimeDiff = Convert.ToInt32(timeDiff),
                        PageNo = req.PageNo
                    };
                    if (req.InteractionType == 2)
                    {
                        var data = da.GetMyLeads(de);
                        var response = new GetInteractionDetailsResponseDTO();
                        var leads =
                             data[0].Select(
                                 d => GenericHelper.CopyObject<DEBCApiLeadDBResponse, GetInteractionDetailResponseDTO>(d));
                        response.interactionDetails = leads.ToList();
                        response.MaxNumberOfPages = data[1][0].MaxNumberOfPages;
                        response.CurrentPage = data[1][0].CurrentPage;
                        response.TimeZone = req.TimeZone;
                        return ApiHelper.CreateSuccessResponse(this, response, response.interactionDetails.Count > 0 ? "" : "No record(s) found", response.interactionDetails.Count > 0 ? 1 : 1);
                    }
                    else
                    {
                        var data = da.GetList<GetInteractionDetailResponseDTO>(de);
                        var response =
                        new
                        {
                            MaxNumberOfPages = 0,
                            CurrentPage = 0,
                            TimeZone = req.TimeZone,
                            interactionDetails = data
                        };
                        return ApiHelper.CreateSuccessResponse(this, response, response.interactionDetails.Count > 0 ? "" : "No record(s) found", response.interactionDetails.Count > 0 ? 1 : 1);
                        //var data = da.GetLists<GetInteractionDetailResponseDTO>(de);
                        //var response = new
                        //{
                        //    MaxNumberOfPages = 0,
                        //    CurrentPage = 0,
                        //    interactionDetails = data.ToList(),
                        //};
                        //return ApiHelper.CreateSuccessResponse(this, response, response.interactionDetails.Count > 0 ? "" : "No record(s) found", response.interactionDetails.Count > 0 ? 1 : 1);
                    }
                    //var response = new GetInteractionDetailsResponseDTO();


                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Saves call details
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     {
        ///         "leadID": 2502798,
        ///         "callDuration": 27,
        ///         "empCode": "SDC1216",
        ///         "phoneNo": "+919582714494",
        ///         "ringingTime": 10,
        ///         "CallActivityId": 0,
        ///         "CalledTo":2502798,
        ///         "CallType":"Lead/Employee"   
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///      {
        ///          "status": 1,
        ///          "message": "Call Details Saved Successfully",
        ///          "data": {
        ///              "callActivityId": 420724
        ///          }
        ///      }
        ///     
        /// </remarks>
        /// <returns>List of call details</returns>
        [HttpPost]
        [Route("SaveLeadCallDetails")]
        [AllowAnonymous]
        public IActionResult SaveLeadCallDetails([FromBody] SaveLeadCallDetailsRequestDTO req)
        {
            try
            {
                ApiHelper.ValidateModelState(ModelState);
                StringValues authorization = "";
                HttpContext.Request.Headers.TryGetValue("authorization", out authorization);
                var token = authorization.ToString().Replace("Bearer ", "");
                DABCApiLogin daLogin = new DABCApiLogin();
                DEBCApiLogin deLogin = new DEBCApiLogin()
                {
                    CallValue = DEBCApiLoginCallValues.ValidateToken,
                    Token = token
                };

                var validToken = daLogin.ValidateToken(deLogin);

                var calledBy = req.CalledBy;
                if (validToken.UserId != 0)
                {
                    calledBy = validToken.UserId;
                }
                if (req.CalledTo==0 && req.LeadID==0)
                {
                    throw new ApplicationException("CalledTo/LeadId can not be left blank!");
                }
                DABCApiLead da = new DABCApiLead();
                DEBCApiLead de = new DEBCApiLead()
                {
                    CallValue = DEBCApiLeadCallValues.SaveCallDetails,
                    CallDuration = req.CallDuration,
                    RingDuration = req.RingingTime,
                    LeadId = req.LeadID,
                    PhoneNumber = req.PhoneNo,
                    CurrentUser = currentUser,
                    CalledBy = calledBy,
                    CallActivityID = req.CallActivityId,
                    CalledTo = req.CalledTo,
                    CallType = req.CallType
                };

                var callActivityId = da.SaveCallDetails(de);

                if (callActivityId == 0)
                    throw new ApplicationException("Unable to fetch CallActivityID");
                return Ok(new
                {
                    Status = 1,
                    Message = "Call Details Saved Successfully",
                    Data = new
                    {
                        CallActivityId = callActivityId,
                    }
                });
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Update lead Details
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///			{
        ///				 "leadId":"1067437",
        ///				 "BudgetDesc": "test",
        ///				 "developer": "Godrej Next",
        ///			     "project": "Godrej Hoodi",
        ///			     "city": "Bangalore"
        ///			}
        ///		
        /// Sample Response:
        ///
        ///     {
        ///         "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "message": "Updated Successfully"
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>Success Message</returns>
        [HttpPost]
        [Route("UpdateLeadDetails")]
        public IActionResult UpdateLeadDetails([FromBody] UpdateLeadDetailsRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.UpdateLeadDetails,
                        Developer = req.Developer,
                        Project = req.Project,
                        BudgetID = req.BudgetID,
                        BudgetDesc=req.BudgetDesc,
                        CurrentUser = currentUser,
                        City = req.City,
                        LeadId = req.LeadID
                    };
                    var data = da.GetList<DEBCApiLeadDBResponse>(de);

                    return ApiHelper.UpdateSuccessResponse(this, "Successfully Updated");
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Update lead Details
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///			{
        ///				 "leadId":"1067437",
        ///				 "salutation": "Mr.",
        ///				 "leadName": "shahid",
        ///			     "website": "www.google.co.in",
        ///			     "state": 10,
        ///			     "country": 11,
        ///			     "address": "Gurgaon Sec-44",
        ///			     "city": 12,
        ///			     "zipCode": 110042,
        ///			     "industry": "Square Capital",
        ///			     "CityName": "Delhi",
        ///			     "CompanyName": "Tata Housing",
        ///			     "Title": "Trademark"
        ///			}
        ///		
        /// Sample Response:
        ///
        ///     {
        ///         "status": 1,
        ///         "message": "Updated Successfully",
        ///     }
        ///     
        /// </remarks>
        /// <returns>Success Message</returns>
        [HttpPost]
        [Route("UpdateLeadBasicInfo")]
        public IActionResult UpdateLeadBasicInfo([FromBody] UpdateLeadBasicInfoRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.UpdateLeadBasicInfo,
                        Salutation = req.Salutation,
                        FirstName = req.LeadName,
                        Website = req.Website,
                        State = req.State,
                        Country = req.Country,
                        Zipcode = req.ZipCode,
                        Industry = req.Industry,
                        Address = req.address,
                        CurrentUser = currentUser,
                        CityId = req.City,
                        LeadId = req.LeadID,
                        City = req.CityName,
                        CompanyName = req.CompanyName,
                        Title = req.Title,
                    };
                    var data = da.GetList<DEBCApiLeadDBResponse>(de);

                    return ApiHelper.UpdateSuccessResponse(this, "Successfully Updated");
                }
                else
                {
                    //var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    var error = ModelState.Values.FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        /// <summary>
        /// Update LeadAllowed Status
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///         {
        ///         "empId":"29",
        ///         "status": true
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "Successfully Changed",
        ///         }
        ///     
        /// </remarks>
        /// <returns>Update LeadAllowed Status</returns>
        [HttpPost]
        [Route("UpdateLeadAllowedStatus")]
        public IActionResult UpdateLeadAllowedStatus([FromBody] UpdateLeadAllowedStatusRequestDTO req)
        {

            var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {

                        CallValue = DEBCApiLeadCallValues.UpdateLeadAllowedStatus,
                        CurrentUser = currentUser,
                        EmpId = req.EmpId,
                        Status = req.Status == true ? 1 : 0

                    };
                    var data = da.GetList<DEUpdateLeadAllowedStatusResponseDTO>(de);
                    if (data.Count > 0)
                    {
                        return ApiHelper.UpdateSuccessResponse(this, data[0].Message);
                    }
                    else
                    {
                        return ApiHelper.CreateSuccessResponse(this, data);
                    }

                }
                else
                {
                    var modelError = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0];
                    var error = modelError.ErrorMessage == "" ? modelError.Exception.Message : modelError.ErrorMessage;

                    return Ok(error);
                    // throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Send Otp  
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///         {
        ///         "leadActivityId":"8844060",
        ///         "CurrentUser":"760",
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "message": "Feedback Code Sent successfully."
        ///             }
        ///         }
        ///     
        /// </remarks>
        /// <returns>Send Otp</returns>
        [HttpPost]
        [Route("SendOtp")]
        [AllowAnonymous]
        public IActionResult SendOtp([FromBody] LeadMeetingOTPDTO req)
        {
            _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Inside Send Otp Function");
            try
            {
                ApiHelper.ValidateModelState(ModelState);
                StringValues authorization = "";
                HttpContext.Request.Headers.TryGetValue("authorization", out authorization);
                var token = authorization.ToString().Replace("Bearer ", "");
                DABCApiLogin daLogin = new DABCApiLogin();
                DEBCApiLogin deLogin = new DEBCApiLogin()
                {
                    CallValue = DEBCApiLoginCallValues.ValidateToken,
                    Token = token
                };
                var validToken = daLogin.ValidateToken(deLogin);
                _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " called validate token");
                var calledBy = req.CurrentUser;
                if (validToken.UserId != 0)
                {
                    calledBy = validToken.UserId;
                }

                if (ModelState.IsValid)
                {
                    _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Validate Model");
                    DABCApiOTP da = new DABCApiOTP();
                    DEBCApiOTP de = new DEBCApiOTP()
                    {
                        CallValue = DEBCApiOTPCallValues.GenrateOTP,
                        CurrentUser = calledBy,
                        LeadActivityRecNo = req.leadActivityId,
                        OTP = req.OTP
                    };
                    var data = da.GetList<DEBCApiOTPDBResponse>(de);

                    _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Called Generate OTP");

                    if (data.Count > 0)
                    {
                        var sendOTPSMS = data[0].SendOTPSMS;
                        _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Sending Email");
                        try
                        {
                            EmailTemplate _path;
                            _path = AppSettingsConf.EMailTemplatePath(MailDetails.OTP);
                            if (data[0].PWAExists)
                            {
                                _path = AppSettingsConf.EMailTemplatePath(MailDetails.OTPPWA);
                            }
                            var OTPTemplate = _hostingEnvironment.ContentRootPath + _path.TemplateUrl;
                            //var OTPTemplate = _hostingEnvironment.ContentRootPath + @"\resources\EmailTemplates\CRMBCApiOTP.html";
                            Console.WriteLine(OTPTemplate);
                            string mailBody = string.Empty;
                            using (StreamReader reader = new StreamReader(OTPTemplate))
                            {
                                mailBody = reader.ReadToEnd();
                            }
                            var row = data;

                            //var sendEmail = Convert.ToString(data[0].Email);
                            mailBody = mailBody
                                .Replace("{CustomerName}", Convert.ToString(data[0].CustomerName))
                                .Replace("{RMName}", Convert.ToString(data[0].RMName))
                                .Replace("{HappyOtp}", Convert.ToString(data[0].HappyOTP))
                                .Replace("{unHappyOtp}", Convert.ToString(data[0].UnhappyOTP))
                                .Replace("{url}", Convert.ToString(data[0].PWAUrl))
                                .Replace("{ActivityType}", Convert.ToString(data[0].ActivityName))
                                .Replace("{RMName}", Convert.ToString(data[0].RMName));
                            //.Replace("{SharedComments}", Convert.ToString(data[0].SharedComments));

                            var mailTo = Convert.ToString(data[0].Email);
                            var mailCc = Convert.ToString(data[0].MailCc);
                            var mailBcc = Convert.ToString(data[0].MailBcc);
                            // var mailSubject = "OTP for Meeting";
                            var mailSubject = _path.Subject
                                .Replace("{ActivityType}", Convert.ToString(data[0].ActivityName))
                                .Replace("{RMName}", Convert.ToString(data[0].RMName));

                            SendMail.f_sendMailFromCRM(mailTo, mailCc, mailBcc, "", mailBody, mailSubject, null);
                            _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Sending Email Faiiled");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Exception" + ex.Message);
                        }


                        _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Sending SMS");
                        if (sendOTPSMS)
                        {
                            var data0 = data[0];
                            sendsms(req.leadActivityId, data0.CountryCode,
                                data0.PhoneNumber,
                                data0.HappyOTP.ToString(),
                                data0.UnhappyOTP.ToString(),
                                data0.RMShortName,
                                data0.PWAUrl,
                                data0.TryCount);
                        }
                        else
                        {
                            _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " SMS SendOTPSMS is false");
                        }
                        _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " SMS Sent");

                        //***********      Send Notification  ***********//
                        try
                        {
                            var sendNotification = data[0].NVWebPush;
                            _logger.LogInformation("Debugging Notification RecNo: " + req.leadActivityId + " Sending Notification");
                            var _path = AppSettingsConf.SendNotificationSettings(NotificationType.Notification);
                            var message = _path.Message
                                .Replace("{FirstName}",GenericHelper.ShortName(data[0].RMName))
                                .Replace("{HappyOtp}",Convert.ToString(data[0].HappyOTP))
                                .Replace("{UnHappyOtp}", Convert.ToString(data[0].UnhappyOTP));
                            var title = _path.Title;
                            if (sendNotification)
                            {
                                try
                                {
                                    Console.WriteLine("Sending Notification....");
                                    //var userId = "919818437359";
                                    bool response = GenericHelper.NVWebPushToApi(title, message, data[0].PWAUrl, data[0].UserId);
                                    if (response)
                                        Console.WriteLine("Notification Sent");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogInformation("Debugging WebPushApi RecNo: " + req.leadActivityId + " Exception" + ex.Message);
                                    Console.WriteLine("WebPushApi \n" + ex);
                                }
                            }
                            else
                            {
                                Console.Write(" :Not sending Notification");
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Debugging ActivityNotification RecNo: " + req.leadActivityId + " Exception" + ex.Message);
                            Console.WriteLine("Exception ActivityNotification- Sending Notification\n" + ex);
                        }
                        return ApiHelper.UpdateSuccessResponse(this, "Feedback Code sms/mail/notification sent successfully.");
                    }
                    else
                    {
                        return ApiHelper.CreateSuccessResponse(this, data);
                    }

                }
                else
                {
                    var modelError = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0];
                    var error = modelError.ErrorMessage == "" ? modelError.Exception.Message : modelError.ErrorMessage;

                    return ApiHelper.CreateErrorResponse(this, error);
                    // throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Verify OTP
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///         {
        ///         "leadActivityId":"8844060",
        ///         "CurrentUser":"760",
        ///         "OTP":5555
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "Feedback Code Matched Succedfully",
        ///             "data": {
        ///                 "leadActivityRecNo": 8844060
        ///             }
        ///         }
        ///     
        /// </remarks>
        /// <returns>Verify Otp</returns>
        [HttpPost]
        [Route("VerifyOTP")]
        [AllowAnonymous]
        public IActionResult VerifyOTP([FromBody] LeadMeetingOTPDTO req)
        {

            //var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                ApiHelper.ValidateModelState(ModelState);
                StringValues authorization = "";
                HttpContext.Request.Headers.TryGetValue("authorization", out authorization);
                var token = authorization.ToString().Replace("Bearer ", "");
                DABCApiLogin daLogin = new DABCApiLogin();
                DEBCApiLogin deLogin = new DEBCApiLogin()
                {
                    CallValue = DEBCApiLoginCallValues.ValidateToken,
                    Token = token
                };

                var validToken = daLogin.ValidateToken(deLogin);
                var calledBy = req.CurrentUser;
                if (validToken.UserId != 0)
                {
                    calledBy = validToken.UserId;
                }
                if (ModelState.IsValid)
                {
                    DABCApiOTP da = new DABCApiOTP();
                    DEBCApiOTP de = new DEBCApiOTP()
                    {

                        CallValue = DEBCApiOTPCallValues.VerifyOTP,
                        CurrentUser = calledBy,
                        LeadActivityRecNo = req.leadActivityId,
                        OTP = req.OTP
                    };
                    var data = da.GetList<DEBCApiOTPDBResponse>(de);
                    var response = new
                    {
                        leadActivityId = req.leadActivityId,
                    };
                   
                    /***  Mail to supervisor on unhappy interaction feedback by a customer    ***/

                    if (data.Count > 0 && data[0].UnHappy == 1)
                    {
                        try
                        {
                            var unHappyEmailDetails = AppSettingsConf.EMailTemplatePath(MailDetails.UnhappyMailToSup);
                            var emailTemplate = _hostingEnvironment.ContentRootPath + unHappyEmailDetails.TemplateUrl;
                            string mailBody = string.Empty;
                            using (StreamReader reader = new StreamReader(emailTemplate))
                            {
                                mailBody = reader.ReadToEnd();
                            }
                            var row = data[0];

                            var mailSubject = unHappyEmailDetails.Subject
                                .Replace("{RMName}", row.RMName.ToString());
                            mailBody = mailBody
                                .Replace("{Supervisor}", row.SupervisorName)
                                .Replace("{RMName}", row.RMName)
                                .Replace("{LeadID}", row.LeadID.ToString())
                                .Replace("{CustomerName}", row.CustomerName)
                                .Replace("{LeaderName}",row.LeaderName);
                            var mailTo = Convert.ToString(row.Email);
                            var mailCc = Convert.ToString(row.MailCc);
                            var mailBcc = Convert.ToString(row.MailBcc);
                            SendMail.f_sendMailFromCRM(mailTo, mailCc, mailBcc, "", mailBody, mailSubject, null);
                          //  Console.WriteLine("Mail sent successfully to supervisor");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Debugging UnHappyMailToSup:" + validToken.UserId + " Exception" + ex.Message);
                            Console.WriteLine(" Error UnHappy Mail To Supervisor \n" + ex);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Happy");
                    }

                    //------------Send Notification on Mobile App-------------------------------
                    HttpClient client = new HttpClient();                    client.DefaultRequestHeaders.TryAddWithoutValidation("api_key", AppSettingsConf.BeatsApiKeys().api_key);                    string postData = "{'leadActivityId':" + req.leadActivityId + "}";                    var content = new StringContent(postData, System.Text.Encoding.UTF8, "application/json");                    string apiUrl = AppSettingsConf.BeatsApiKeys().api_url + "NotificationCenter/HappyandUnhappyNotification";                    HttpResponseMessage responses = client.PostAsync(apiUrl, content).Result;

                    return ApiHelper.CreateSuccessResponse(this, response, "Feedback Code Matched Succedfully", 1);
                }
                else
                {
                    var modelError = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0];
                    var error = modelError.ErrorMessage == "" ? modelError.Exception.Message : modelError.ErrorMessage;

                    return ApiHelper.CreateErrorResponse(this, error);
                    // throw new ApplicationException(error);
                }
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }


        private void CreateOtp(int LeadActivityRecNo, int OTP)
        {
            _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " Inside Create Otp Function - OTP: " + OTP);
            //var currentUser = Convert.ToInt32(HttpContext.Items["CurrentUserID"]);
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiOTP da = new DABCApiOTP();
                    DEBCApiOTP de = new DEBCApiOTP()
                    {

                        CallValue = DEBCApiOTPCallValues.GenrateOTP,
                        CurrentUser = currentUser,
                        LeadActivityRecNo = LeadActivityRecNo,
                        OTP = OTP
                    };
                    _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " Before Calling Save OTP");
                    // _logger.LogInformation("Before Save Otp for LeadActivityRecNo {0}", LeadActivityRecNo );
                    var data = da.GetList<DEBCApiOTPDBResponse>(de);
                    // _logger.LogInformation("After Save Otp data count {0}",data.Count );

                    var data0 = data[0];
                    var countryCode = (data[0].CountryCode);
                    var phoneNumber = (data[0].PhoneNumber);
                    var happyOTP = Convert.ToString(data[0].HappyOTP);
                    var unhappyOTP = Convert.ToString(data[0].UnhappyOTP);
                    
                    sendsms(LeadActivityRecNo, countryCode, phoneNumber, happyOTP, unhappyOTP, data0.RMShortName, data0.PWAUrl, data0.TryCount);
                    _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " Inside Create Otp Function - countryCode: " 
                        + countryCode + " phoneNumber: " + phoneNumber + ", HappyOTP: " + happyOTP + ", UnhappyOTP: " + unhappyOTP);

                    
                    if (data.Count > 0)
                    {
                        var sendOTPSMS = data[0].SendOTPSMS;
                        var _path = AppSettingsConf.EMailTemplatePath(MailDetails.OTP);
                        if (data[0].PWAExists)
                        {
                            _path = AppSettingsConf.EMailTemplatePath(MailDetails.OTPPWA);
                        }
                        var OTPTemplate = _hostingEnvironment.ContentRootPath + _path.TemplateUrl;
                        //var OTPTemplate = _hostingEnvironment.ContentRootPath + @"\resources\EmailTemplates\CRMBCApiOTP.html";
                        Console.WriteLine(OTPTemplate);
                        string mailBody = string.Empty;
                        using (StreamReader reader = new StreamReader(OTPTemplate))
                        {
                            mailBody = reader.ReadToEnd();

                        }
                        var row = data;

                        //var sendEmail = Convert.ToString(data[0].Email);
                        mailBody = mailBody
                            .Replace("{CustomerName}", Convert.ToString(data[0].CustomerName))
                            .Replace("{FOSName}", Convert.ToString(data[0].FOSName))
                            .Replace("{HappyOtp}", Convert.ToString(data[0].HappyOTP))
                            .Replace("{unHappyOtp}", Convert.ToString(data[0].UnhappyOTP))
                            .Replace("{url}", Convert.ToString(data[0].PWAUrl))
                            .Replace("{ActivityType}", Convert.ToString(data[0].ActivityName))
                            .Replace("{RMName}", Convert.ToString(data[0].RMName));
                        //.Replace("{SharedComments}", Convert.ToString(data[0].SharedComments));

                        var mailTo = Convert.ToString(data[0].Email);
                        var mailCc = Convert.ToString(data[0].MailCc);
                        var mailBcc = Convert.ToString(data[0].MailBcc);
                        // var mailSubject = "OTP for Meeting";
                        var mailSubject = _path.Subject
                            .Replace("{ActivityType}", Convert.ToString(data[0].ActivityName))
                            .Replace("{RMName}", Convert.ToString(data[0].RMName));

                        SendMail.f_sendMailFromCRM(mailTo, mailCc, mailBcc, "", mailBody, mailSubject, null);

                        //return ApiHelper.UpdateSuccessResponse(this, "OTP Mail Sent Successfully.");


                        // _logger.LogInformation("Debugging OTP RecNo: " + req.leadActivityId + " Sending SMS");
                        
                        if (sendOTPSMS)
                        {
                            data0 = data[0];
                            sendsms(LeadActivityRecNo, data0.CountryCode,
                                data0.PhoneNumber,
                                data0.HappyOTP.ToString(),
                                data0.UnhappyOTP.ToString(),
                                data0.RMShortName,
                                data0.PWAUrl,
                                data0.TryCount);
                        }
                        else
                        {
                            _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " SMS SendOTPSMS is false");
                        }
                        _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " SMS Sent");

                        //***********      Send Notification  ***********//
                        try
                        {
                            var sendNotification = data[0].NVWebPush;
                            _logger.LogInformation("Debugging Notification RecNo: " + LeadActivityRecNo + " Sending Notification");
                            var path = AppSettingsConf.SendNotificationSettings(NotificationType.Notification);
                            var message = path.Message
                                .Replace("{FirstName}", GenericHelper.ShortName(data[0].RMName))
                                .Replace("{HappyOtp}", Convert.ToString(data[0].HappyOTP))
                                .Replace("{UnHappyOtp}", Convert.ToString(data[0].UnhappyOTP));
                            var title = path.Title;
                            if (sendNotification)
                            {
                                try
                                {
                                    Console.WriteLine("Sending Notification....");
                                    //var userId = "919818437359";
                                    bool response = GenericHelper.NVWebPushToApi(title, message, data[0].PWAUrl, data[0].UserId);
                                    if (response)
                                        Console.WriteLine("Notification Sent");
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogInformation("Debugging WebPushApi RecNo: " + LeadActivityRecNo + " Exception" + ex.Message);
                                    Console.WriteLine("WebPushApi \n" + ex);
                                }
                            }
                            else
                            {
                                Console.Write(" :Not sending Notification");
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogInformation("Debugging ActivityNotification RecNo: " + LeadActivityRecNo + " Exception" + ex.Message);
                            Console.WriteLine("Exception ActivityNotification- Sending Notification\n" + ex);
                        }

                        _logger.LogInformation("Send sms hit true" );
                    }
                    else
                    {
                       
                        //return ApiHelper.CreateErrorResponse(this,"No OTP found");
                    }
                    
                }
                else
                {
                    var modelError = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0];
                    var error = modelError.ErrorMessage == "" ? modelError.Exception.Message : modelError.ErrorMessage;
                    _logger.LogInformation("Create OTP Modelerror {0}", modelError.ErrorMessage);
                    //return ApiHelper.CreateErrorResponse(this, error);
                    _logger.LogInformation("Debugging OTP RecNo: " + LeadActivityRecNo + " Model State Invalid OTP Not Sent" + OTP);
                }
            }

            catch (Exception ex)
            {
                string str = ex.Message + "Inner Exception " + ex.InnerException;
                _logger.LogInformation("HTTP CreateOtp {0}", str);
                //return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }
        private bool sendsms(int leadActivityRecNo, string countrycode, string mobileno, string happyOtp, string uHappyOtp, string rmName = "", string pwaurl = "", int tryCount = 1)
        {
            string smsurl = "";
            bool vflag = false;
            string msg = "";
            _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Inside sendsms");
            try
            {
                var smskey = AppSettingsConf.SMSKeySettings();
                if (smskey.Testing == 1)
                    mobileno = smskey.TestMobileNo;
                if (countrycode.Replace("+", "") == "91")
                {
                    _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Sending Case 1: TryCount: " + tryCount + " CountryCode" + countrycode);
                    var domesticsms = AppSettingsConf.SMSKeySettings();
                    msg = domesticsms.DomesticSMS;

                    if (!string.IsNullOrEmpty(pwaurl))
                        msg = domesticsms.OTPMsgForPWAUrl;

                    //msg = happyOtp + " is your Square Connect Mobile App Verification Code";
                    msg = msg.Replace("{HappyOtp}", happyOtp)
                        .Replace("{UnHappyOtp}", uHappyOtp)
                        .Replace("{RMShortName}", rmName)
                        .Replace("{PWAUrl}", pwaurl);
                    // happyOtp + uHappyOtp + " If you were satisfied with your meeting with our representative please Share OTP Xxxx. For any reason if you were not satisfied please share YYYY.";
                    msg = HttpUtility.UrlEncode(msg);
                    mobileno = HttpUtility.UrlEncode(mobileno); //"919716214179"; 
                    var url = AppSettingsConf.SendSMSSettings(SMSType.SMS);
                    smsurl = url.DomesticUrl;
                    if (smsurl != null)
                    {
                        smsurl = smsurl.Replace("amp;", "").Replace("{MobileNo}", mobileno.Trim()).Replace("{Message}", msg.Trim());
                        // _logger.LogInformation("SMS URL {0}", smsurl);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Url: " + smsurl);
                        HttpResponseMessage response = client.GetAsync(smsurl).Result;
                        _logger.LogInformation("HTTP Response {0}", response);

                        var result = response.Content.ReadAsStringAsync().Result;
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Response Headers: " + response);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Response: " + result);
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode.ToString().ToLower() == "ok")
                                vflag = true;
                        }
                    }
                    else
                    {
                        _logger.LogInformation("smsUrl is null");
                        vflag = false;
                    }
                }
                else if (tryCount == 1 && countrycode.Replace("+", "") != "968")
                {
                    _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Sending Case 2: TryCount: " + tryCount + " CountryCode" + countrycode);
                    var gccsms = AppSettingsConf.SMSKeySettings();
                    msg = gccsms.GCCMsg;
                    if (!string.IsNullOrEmpty(pwaurl))
                        msg = gccsms.OTPMsgForPWAUrl;

                    //msg = happyOtp + " is your Square Connect Mobile App Verification Code";
                    msg = msg.Replace("{HappyOtp}", happyOtp)
                        .Replace("{UnHappyOtp}", uHappyOtp)
                        .Replace("{RMShortName}", rmName)
                        .Replace("{PWAUrl}", pwaurl);
                    // happyOtp + uHappyOtp + " If you were satisfied with your meeting with our representative please Share OTP Xxxx. For any reason if you were not satisfied please share YYYY.";
                    msg = HttpUtility.UrlEncode(msg);
                    mobileno = HttpUtility.UrlEncode(mobileno); //"9716214179"; 
                    var url = AppSettingsConf.SendSMSSettings(SMSType.SMS);
                    smsurl = url.GCCUrl;
                    if (smsurl != null)
                    {
                        smsurl = smsurl.Replace("amp;", "")
                            .Replace("{MobileNo}", mobileno.Trim())
                            .Replace("{Message}", msg.Trim())
                            .Replace("{CountryCode}", countrycode.Replace("+", ""))
                            .Replace("{RMShortName}", rmName)
                            .Replace("{PWAUrl}", pwaurl);
                        // _logger.LogInformation("SMS URL {0}", smsurl);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Url: " + smsurl);
                        HttpResponseMessage response = client.GetAsync(smsurl).Result;
                        _logger.LogInformation("HTTP Response {0}", response);

                        var result = response.Content.ReadAsStringAsync().Result;
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Response Headers: " + response);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Response: " + result);
                        if (response.IsSuccessStatusCode)
                        {
                            if (response.StatusCode.ToString().ToLower() == "ok")
                                vflag = true;
                        }
                        else
                        {
                            SendMail.f_sendMailCommon("pankaj.sharma1@squareyards.co.in,farookh.mansuri@squareyards.co.in", null, null, null,
                                response.Content.ToString(),
                                "MShastra Failed for CountryCode: " + countrycode + " RecNo: " + leadActivityRecNo,
                                MailType.AWS, EmailFrom.NoReply);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("smsUrl is null");
                        vflag = false;
                    }
                }
                else
                {
                    _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Sending Case 3: TryCount: " + tryCount + " CountryCode" + countrycode);
                    string number = mobileno;
                    msg = happyOtp;
                    msg = HttpUtility.UrlEncode(msg);
                    number = HttpUtility.UrlEncode(number);
                    var url = AppSettingsConf.SendSMSSettings(SMSType.SMS);
                    smsurl = url.GlobalUrl;
                    if (smsurl != null)
                    {
                        var Globalsms = AppSettingsConf.SMSKeySettings();
                        var GlobalHappy = Globalsms.GlobalHappysms;
                        var GlobalUnHappy = Globalsms.GlobalUnHappysms;
                        var Happyurl = smsurl.Replace("{api}", url.Apikey).Replace("{MobileNo}", number.Trim()).Replace("{otp}", happyOtp).Replace("{msgTemplate}", GlobalHappy);

                        var UnHappyurl = smsurl.Replace("{api}", url.Apikey).Replace("{MobileNo}", number.Trim()).Replace("{otp}", uHappyOtp).Replace("{msgTemplate}", GlobalUnHappy);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Url: " + smsurl);
                        HttpResponseMessage Happyresponse = client.GetAsync(Happyurl).Result;
                        HttpResponseMessage unHappyresponse = client.GetAsync(UnHappyurl).Result;

                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + "HTTP HappyResponse {0}", Happyresponse);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + "GolabalunHappyurl {0}", UnHappyurl);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + "GolabalHappyurl {0}", Happyurl);
                        _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + "HTTP UnhappyResponse {0}", Happyresponse);
                    }
                    else
                    {
                        _logger.LogInformation("smsUrl is null");
                        vflag = false;
                    }
                }
            }
            catch (Exception ex)
            {
                string str = ex.Message + "Inner Exception " + ex.InnerException;
                _logger.LogInformation("HTTP Exception {0}", str);
                //while (ex.InnerException != null) ex = ex.InnerException;
                //_logger.LogInformation("HTTP Inner Exception {0}", ex.Message);
                _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Exception" + ex.Message);
            }
            _logger.LogInformation("Debugging OTP RecNo: " + leadActivityRecNo + " Existing sendsms");
            return vflag;
        }
        private string getLeadSection(int noOfDay)
        {
            return noOfDay == 0 ? "Today"
                : noOfDay == 1 ? "Yesterday"
                : (noOfDay > 1 && noOfDay <= 8) ? "Last Week"
                : (noOfDay > 8 && noOfDay <= 25) ? "Last Month"
                : "Older";
        }

        /// <summary>
        /// gupshup sms logs
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///         {
        ///         "deliveredTS":"2018-04-16 15:33:01",
        ///         "status":"OK",
        ///         "phoneNo":"9956787633"
        ///         }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "Saved Successfully",
        ///             "data": {
        ///             }
        ///         }
        ///     
        /// </remarks>
        /// <returns>gupshup sms logs</returns>
        [HttpPost, AllowAnonymous, Route("SendOtpresponse")]
        public IActionResult SendOtpresponse([FromBody] SendOTPDTO req)
        {
            try
            {
                var cnnString = AppSettingsConf.GetConnectionString("beatscrm");
                var cmdd = "INSERT INTO gupshup_sms_logs(deliveredTS, status, phoneNo) VALUES(@deliveredTS,@status,@phoneNo)";
                using (MySqlConnection cnn = new MySqlConnection(cnnString))
                {
                    using (MySqlCommand cmd = new MySqlCommand(cmdd, cnn))
                    {
                        cmd.Parameters.AddWithValue("@deliveredTS",req.deliveredTS);
                        cmd.Parameters.AddWithValue("@status", req.status);
                        cmd.Parameters.AddWithValue("@phoneNo", req.phoneNo);

                        cnn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
                return ApiHelper.CreateSuccessResponse(this, "Saved Successfully");
            }

            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Get All Comments
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///    
        ///     {
        ///      "pageno":1,
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///         {
        ///             "status": 1,
        ///             "message": "",
        ///             "data": {
        ///                 "comments": "hzjzjdjjckckckckjcjdjjxj"
        ///                 }
        ///         }
        ///     
        /// </remarks>
        /// <returns>List of Comments</returns>

        [HttpPost]
        [Route("Getcomments")]
        public IActionResult Getcomments([FromBody]GetMyLeadsRequestDTO req)
        {

            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetLeadActivityComments,
                        PageNo = req.PageNo
                    };

                    var response = da.GetLeadComments(de);
                    //var response = new
                    //{
                    //    Comments = data
                    //};

                    return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }



        }

        /// <summary>
        /// Gets Lead Info by PhoneNumber
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /GetLeadsInfo
        ///     {
        ///       "phoneNumber":"9246516915"
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///     POST /GetMyLeads
        ///     {
        ///       "status": 1,
        ///       "message": "",
        ///       "data": [
        ///       {
        ///             "leadID": 2411210,
        ///             "project": "Other",
        ///             "name": "Satyavrit Gaur",
        ///             "phoneNumber": "85298616729",
        ///             "assignedTo": "Sonali Gupta (SYME041)",
        ///             "status": "Warm",
        ///             "sharedWith": "",
        ///             "countryCode": "91"
        ///       }
        ///       ]    
        ///     }
        ///     
        /// </remarks>
        /// <returns>Lead Info by PhoneNumber</returns>
        [HttpPost]
        [Route("GetLeadsInfo")]
        public IActionResult GetLeadsInfo([FromBody] GetLeadInfoRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetLeadInfoByPhoneNumber,
                        PhoneNumber=req.PhoneNumber.Replace("+91","").Replace("+",""),
                        CurrentUser= currentUser
                    };
                    var response = new GetMyLeadInfoByPhoneNumberResponseDTO();
                    var data = da.GetMyLeads(de);
                    var leads =
                        data[0].Select(
                            d => GenericHelper.CopyObject<DEBCApiLeadDBResponse, GetMyLeadInfoByPhoneNumberResponseDTO>(d));
                    return ApiHelper.CreateSuccessResponse(this, leads);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message + "\n" + ex.StackTrace);
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Initiate Call On LeadID
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /InitiateCall
        ///     {
        ///         "leadId": 123456
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///     POST /InitiateCall
        ///     {
        ///       "status": 1,
        ///       "message": "Call Initiate Updated",
        ///       "data": {}
        ///     }
        ///     
        /// </remarks>
        /// <returns>Initiate Call On LeadID</returns>
        [HttpPost]
        [Route("InitiateCall")]
        public IActionResult InitiateCall([FromBody] InitiateCallRequestDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.UpdateCallInitiated,
                        LeadId = req.LeadId,
                        CurrentUser = currentUser
                    };
                    var response = new GetMyLeadInfoByPhoneNumberResponseDTO();
                    var data = da.GetMyLeads(de);
                    
                    return ApiHelper.CreateSuccessResponse(this, "Call Initiate Updated");
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message + "\n" + ex.StackTrace);
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        #region unclaimed leads
        /// <summary>
        /// Gets Unclaimed Leads
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     GET /GetUnclaimedLeads
        ///		
        /// Sample Response:
        /// 
        ///     GET /GetUnclaimedLeads
        ///     {
        ///         "status": 1,
        ///         "message": "",
        ///         "data": {
        ///             "leads": [
        ///                 {
        ///                     "leadId": 3983728,
        ///                     "countryCode": 91
        ///                     "phoneNumber": "9803834299",
        ///                     "customerName": "vikas duhan",
        ///                     "projectName": "Gaur Yamuna City 16th Park View",
        ///                     "hasMeeting": false,
        ///                     "meetingVenue": null,
        ///                     "meetingTime": "0001-01-01T00:00:00",
        ///                     "leadStatus": "Unclaimed",
        ///                     "claimedBy": 0
        ///                 }
        ///             ],
        ///             "callExpireDuration": 60
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>Gets Unclaimed Leads</returns>
        [HttpGet]
        [Route("GetUnclaimedLeads")]
        public IActionResult GetUnclaimedLeads()
        {
            try
            {
                if (ModelState.IsValid)
                {
                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.GetUnclaimedLeads,
                        CurrentUser = currentUser
                    };
                    var data = da.GetList<BCApiLeadDBResponse>(de);

                    var response = new
                    {
                        Leads = data.Select(d => new
                        {
                            d.LeadId,
                            d.CountryCode,
                            d.PhoneNumber,
                            d.CustomerName,
                            d.ProjectName,
                            d.HasMeeting,
                            d.MeetingVenue,
                            d.MeetingTime,
                            d.LeadStatus,
                            d.ClaimedBy
                        }),
                        CallExpireDuration = 60
                    };
                    return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message + "\n" + ex.StackTrace);
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }

        /// <summary>
        /// Update Unclaimed Lead
        /// </summary>
        /// <param name="req"></param>
        /// <param name="authorization"></param>
        /// /// <remarks>
        /// Sample request:
        /// 
        ///     POST /UpdateUnclaimedLead
        ///     {
        ///         "leadId": 123456,
        ///         "leadStatus": "ClaimedNotCalled"
        ///     }
        ///		
        /// Sample Response:
        /// 
        ///     POST /UpdateUnclaimedLead
        ///     {
        ///         "status": 1,
        ///         "message": "Successfully Updated",
        ///         "data": {
        ///             "leadId": 123456,
        ///             "leadStatus": "ClaimedNotCalled",
        ///             "claimedBy": 705,
        ///             "errorMessage": "Lead already claimed"
        ///         }
        ///     }
        ///     
        /// </remarks>
        /// <returns>Update Unclaimed Lead</returns>
        [HttpPost]
        [Route("UpdateUnclaimedLead")]
        public IActionResult UpdateUnclaimedLead([FromBody] UpdateUnclaimedLeadDTO req)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var leadStatus = -1;
                    switch(req.LeadStatus.ToLower())
                    {
                        case "claimednotcalled":
                            leadStatus = -1;
                            break;
                        case "unclaimed":
                            leadStatus = 0;
                            break;
                        default:
                            throw new ApplicationException("LeadStatus can be ClaimedNotCalled/Unclaimed");
                    }

                    DABCApiLead da = new DABCApiLead();
                    DEBCApiLead de = new DEBCApiLead()
                    {
                        CallValue = DEBCApiLeadCallValues.UpdateUnclaimedLead,
                        CurrentUser = currentUser,
                        LeadStatus = leadStatus,
                        LeadId = req.LeadId
                    };
                    var data = da.GetList<BCApiLeadDBResponse>(de);

                    var response = new {
                        data[0].LeadId,
                        data[0].LeadStatus,
                        data[0].ClaimedBy,
                        data[0].ErrorMessage
                    };
                    return ApiHelper.CreateSuccessResponse(this, response);
                }
                else
                {
                    var error = ModelState.Values.Where(v => v.Errors.Count > 0).FirstOrDefault().Errors[0].ErrorMessage;
                    throw new ApplicationException(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:" + ex.Message + "\n" + ex.StackTrace);
                return ApiHelper.CreateErrorResponse(this, ex.Message);
            }
        }
        #endregion
     
    }
}
