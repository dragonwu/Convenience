﻿using AutoMapper;

using Convience.Entity.Data;
using Convience.Entity.Entity;
using Convience.EntityFrameWork.Repositories;
using Convience.Jwtauthentication;
using Convience.Model.Models.GroupManage;
using Convience.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Convience.Service.GroupManage
{
    public class PositionService : IPositionService
    {
        private readonly IRepository<Position> _positionRepository;

        private readonly IUserRepository _userRepository;

        private readonly IUnitOfWork<SystemIdentityDbContext> _unitOfWork;

        private readonly IMapper _mapper;

        public PositionService(IRepository<Position> positionRepository,
            IUserRepository userRepository,
            IUnitOfWork<SystemIdentityDbContext> unitOfWork,
            IMapper mapper)
        {
            _positionRepository = positionRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<bool> AddPositionAsync(PositionViewModel model)
        {
            try
            {
                var entity = _mapper.Map<Position>(model);
                await _positionRepository.AddAsync(entity);
                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int Count()
        {
            return _positionRepository.Get().Count();
        }

        public async Task<bool> DeletePositionAsync(int id)
        {
            await _unitOfWork.StartTransactionAsync();
            try
            {
                var claims = _userRepository.GetUserClaims()
                    .Where(c => c.ClaimType == CustomClaimTypes.UserPosition &&
                    c.ClaimValue == id.ToString());
                _userRepository.GetUserClaims().RemoveRange(claims);

                await _positionRepository.RemoveAsync(id);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
                return false;
            }
        }

        public IEnumerable<PositionResult> GetAllPosition()
        {
            var positions = _positionRepository.Get().OrderBy(p => p.Sort).ToArray();
            return _mapper.Map<Position[], IEnumerable<PositionResult>>(positions);
        }

        public async Task<PositionResult> GetPositionAsync(int id)
        {
            var entity = await _positionRepository.GetAsync(id);
            return _mapper.Map<PositionResult>(entity);
        }

        public IEnumerable<PositionResult> GetPositions(PositionQuery query)
        {
            var positions = _positionRepository.Get(p => true, p => p.Sort,
                query.Page, query.Size).ToArray();
            return _mapper.Map<Position[], IEnumerable<PositionResult>>(positions);
        }

        public async Task<bool> UpdatePositionAsync(PositionViewModel model)
        {
            try
            {
                var entity = await _positionRepository.GetAsync(model.Id);
                _mapper.Map(model, entity);
                _positionRepository.Update(entity);
                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
