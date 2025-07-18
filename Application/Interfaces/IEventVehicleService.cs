﻿using Application.Models.Requests;
using Application.Models.Responses;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEventVehicleService
    {

        Task<EventVehicleDto> AddAsync(AddEventVehicleRequest request);
        Task<IEnumerable<EventVehicleDto>> GetAllAsync();
        Task<List<EventVehicleDto>> GetVehiclesByEventAsync(int eventId);
        Task<EventVehicleDto> GetEventVehicleByIdAsync(int eventVehicleId);
        Task<IEnumerable<EventVehicle>> GetEventVehiclesByUserIdAsync(int userId);
        Task<EventVehicle> GetByIdAsync(int id);
        Task<EventVehicle> UpdateEventVehicleAsync(UpdateEventVehicleRequest eventVehicle, int userId);
        Task<bool> ToggleStatusAsync(int eventVehicleId);
        Task<List<EventVehicleDto>> GetAllEventVehiclesActiveByEventAsync(int eventId);

    }
}
