﻿using AutoMapper;
using backend.entity.backend.api;
using Backend.Model.backend.api.Models.SystemManage;

namespace backend.model.backend.api
{
    public class ModelProfile : Profile
    {
        public ModelProfile()
        {

            CreateMap<RoleViewModel, SystemRole>();
            CreateMap<SystemRole, RoleResult>();
        }
    }
}
