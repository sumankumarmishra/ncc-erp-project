﻿using Abp.Application.Services.Dto;
using NccCore.Anotations;
using NccCore.Uitls;
using ProjectManagement.Services.ResourceService.Dto;
using ProjectManagement.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static ProjectManagement.Constants.Enum.ProjectEnum;

namespace ProjectManagement.Services.ResourceRequestService.Dto
{
    public class GetResourceRequestDto : EntityDto<long>
    {
        public string Name { get; set; }
        public ResourceRequestStatus Status { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime TimeNeed { get; set; }
        public DateTime? TimeDone { get; set; }
        public UserLevel Level { get; set; }
        public Priority Priority { get; set; }
        public string CVName { get; set; }

        [ApplySearchAttribute]
        public string PMNote { get; set; }
        [ApplySearchAttribute]
        public string DMNote { get; set; }
        public List<ResourceRequestSkillDto> Skills { get; set; }
        public bool IsRecruitmentSend { get; set; }
        public string RecruitmentUrl { get => CommonUtil.GetPathSendRecuitment(PathRecruitment); }
        public string PathRecruitment { get; set; }
        public PlanUserInfoDto PlanUserInfo { get; set; }
        public long ProjectId { get; set; }
        [ApplySearchAttribute]
        public string ProjectName { get; set; }
        public ProjectType? ProjectType { get; set; }
        public ProjectStatus ProjectStatus { get; set; }
        public string ProjectCode { get; set; }

        public int Quantity { get; set; }
        public string LinkCv { get; set; }

        public string ProjectTypeName
        {
            get
            {
                return ProjectType == null ? null : CommonUtil.ProjectTypeName(ProjectType.Value);
            }

        }

        public string ProjectStatusName
        {
            get
            {
                return CommonUtil.ProjectStatusName(ProjectStatus);
            }
        }

        public string KomuInfo()
        {
            return $"[#{Id}]" +
                $"Project: {ProjectName}\n" +
                $"Skills: {SkillName}\n" +
                $"Level: {LevelName}\n" +
                $"Time Need: {DateTimeUtils.ToString(TimeNeed)}\n" +
                $"Priority: {PriorityName}\n" +
                $"PM Note: {PMNote}\n";
        }
        public List<long> SkillIds
        {
            get
            {
                if (Skills == null)
                {
                    return null;
                }
                return Skills.Select(s => s.Id).ToList();
            }
        }

        public string SkillName
        {
            get
            {
                if (Skills == null)
                {
                    return "";
                }
                return string.Join(", ", Skills.Select(s => s.Name).ToList());
            }
        }

        public string StatusName
        {
            get
            {
                return Enum.GetName(typeof(ResourceRequestStatus), Status);
            }
        }

        public string PriorityName
        {
            get
            {
                return Enum.GetName(typeof(Priority), Priority);
            }
        }

        public string LevelName
        {
            get
            {
                return CommonUtil.UserLevelName(Level);
            }
        }
        [ApplySearchAttribute]
        public string Code { get; set; }
        public string UserRequestName { get; set; }
        [ApplySearchAttribute]
        public string BillCVEmail { get; set; }
        [ApplySearchAttribute]
        public string PlanUserEmail { get; set; }
        public DateTime? CreateAt { get; set; }
        public PlanUserInfoDto BillUserInfo { get; set; }
        public bool IsNewBillAccount { get; set; }
        public bool IsRequiredPlanResource { get; set; }
    }

    public class PlanUserInfoDto
    {
        public long ProjectUserId { get; set; }
        public UserBaseDto Employee { get; set; }
        public DateTime? PlannedDate { get; set; }
        public ProjectUserRole Role { get; set; }
        public string KomuInfo()
        {
            string plannedDateString = PlannedDate.HasValue ? DateTimeUtils.ToString(PlannedDate.Value) : "N/A";

            return $"Employee: {Employee.KomuInfo()}]\n" +
                $"Start Working Date: {plannedDateString}";
        }
        public List<UserSkillDto> UserSkill { get; set; }
    }

    public class RequestCodeDto
    {
        public string Code { get; set; }
        public ResourceRequestStatus Status { get; set; } 

    }
}
