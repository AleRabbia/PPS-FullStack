﻿using Application.Interfaces;
using Application.Models.Requests;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEventVehicleRepository _eventVehicleRepository;

        private readonly IEmailService _emailService;

        public UserService(
        IUserRepository userRepository,
        IVehicleRepository vehicleRepository,
        IEventRepository eventRepository,
        IEventVehicleRepository eventVehicleRepository,IEmailService emailService)
        {
            _userRepository = userRepository;
            _vehicleRepository = vehicleRepository;
            _eventRepository = eventRepository;
            _eventVehicleRepository = eventVehicleRepository;
            _emailService = emailService;
        }
      

        public List<User> GetUsers()
        {
            return _userRepository.ListAsync().Result ?? new List<User>();
        }

        public User? GetUserById(int idUser)
        {
            return _userRepository.GetByIdAsync(idUser).Result;
        }

        public async Task SignUpUser(UserSignUpRequest userSignUpRequest)
        {
            var recoveryCode = GenerateRecoveryCode();

            var user = new User
            {
                FirstName = userSignUpRequest.FirstName,
                LastName = userSignUpRequest.LastName,
                BirthDate = userSignUpRequest.BirthDate,
                IdentificationNumber = userSignUpRequest.DniNumber,
                Email = userSignUpRequest.Email ?? "",
                Password = userSignUpRequest.Password,
                CityId = userSignUpRequest.City,
                ProvinceId = userSignUpRequest.Province,
                IsActive = EntityState.Inactive,
                RecoveryCode = recoveryCode
            };


            await _userRepository.AddAsync(user);

            await _emailService.SendEmailAsync(
                user.Email,
                "🚀 Activación de cuenta en Massivo App",
                $@"
            <p>¡Hola {user.FirstName}!</p>
            <p>Gracias por registrarte. Para activar tu cuenta, ingresá el siguiente código:</p>
            <p style='font-size: 18px; font-weight: bold;'>{recoveryCode}</p>
            <p>⚠️ Si no te registraste, ignorá este mensaje.</p>
            <br/>
            <p>El equipo de soporte de Massivo App.</p>"
            );

        }

        public async Task<bool> ActivateAccountAsync(string email, string code)
        {
            var user = (await _userRepository.ListAsync())
                .FirstOrDefault(u => u.Email == email && u.RecoveryCode == code);

            if (user == null || user.IsActive == EntityState.Active)
                return false;

            user.IsActive = EntityState.Active;
            user.RecoveryCode = null;

            await _userRepository.UpdateAsync(user);
            return true;
        }


        public void UpdateUser(UserUpdateRequest userUpdateRequest, int idUser)
        {
            User? user = _userRepository.GetByIdAsync(idUser).Result;
            if (user == null)
            {
                throw new ArgumentNullException("User not found");
            }

            user.FirstName = userUpdateRequest.FirstName;
            user.LastName = userUpdateRequest.LastName;
            user.IdentificationNumber = userUpdateRequest.DniNumber;
            user.Email = userUpdateRequest.Email ?? user.Email;
            if (!string.IsNullOrEmpty(userUpdateRequest.Password))
            {
                user.Password = userUpdateRequest.Password;
            }
            user.CityId = userUpdateRequest.City;
            user.ProvinceId = userUpdateRequest.Province;

            _userRepository.UpdateAsync(user).Wait();
        }

        public void ChangeUserRole(RoleChangeRequest roleChangeRequest)
        {
            User? user = _userRepository.GetByIdAsync(roleChangeRequest.UserId).Result;
            if (user == null)
            {
                throw new ArgumentNullException("User not found");
            }

            user.Role = roleChangeRequest.NewRole;
            _userRepository.UpdateAsync(user).Wait();
        }

        public void DesactiveUser(int idUser)
        {
            User? user = _userRepository.GetByIdAsync(idUser).Result;
            if (user == null)
            {
                throw new ArgumentNullException("User not found");
            }
            user.IsActive = Domain.Enums.EntityState.Inactive;
            _userRepository.UpdateAsync(user).Wait();
        }

        public async Task<bool> AdminUpdateUserAsync(int userId, AdminUserUpdateRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return false;

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.BirthDate = request.BirthDate;
            user.IdentificationNumber = request.IdentificationNumber;
            user.Email = request.Email;
            user.CityId = request.CityId;
            user.ProvinceId = request.ProvinceId;
            user.Role = request.Role;

            /*if (!string.IsNullOrEmpty(request.Password))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }*/

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task UpdateUser(User user)
        {
            await _userRepository.UpdateAsync(user);
        }

        public async Task<bool> GenerateRecoveryCodeAndSendEmailAsync(string email)
        {
            var user = (await _userRepository.ListAsync()).FirstOrDefault(u => u.Email == email);
            if (user == null) return false;

            var recoveryCode = GenerateRecoveryCode();
            user.RecoveryCode = recoveryCode;
            user.MustChangePassword = true;

            await _userRepository.UpdateAsync(user);

            await _emailService.SendEmailAsync(
                user.Email,
                "🔒 Recuperación de contraseña – Tu clave provisoria",
                $@"
                    <p>Hola,</p>
                    <p>Recibimos tu solicitud para restablecer tu contraseña.</p>
                    <p><strong>Tu clave provisoria es:</strong> <span style='font-size:18px;'>{recoveryCode} 🔑</span></p>
                    <p>⚠️ Si no solicitaste este cambio, ignorá este mensaje.</p>
                    <br/>
                    <p>Saludos,<br/>El equipo de soporte de Massivo App.</p>"
            );


            return true;
        }

        private string GenerateRecoveryCode()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task<bool> ResetPasswordWithRecoveryCodeAsync(string email, string recoveryCode, string newPassword)
        {
            var user = _userRepository.GetUserByEmail(email);

            if (user == null || user.RecoveryCode != recoveryCode || !user.MustChangePassword)
                return false;

            user.Password = newPassword;
            user.RecoveryCode = null;
            user.MustChangePassword = false;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<bool> ToggleStatusAsync(int userId)
        {
            // Verificar el estado actual del usuario
            var currentState = await _userRepository.GetUserEntityStateAsync(userId);
            bool isDeactivating = currentState == EntityState.Active;

            // Si estamos desactivando, desactivar recursos relacionados
            if (isDeactivating)
            {
                // Desactivar vehículos del usuario
                var licensePlates = await _userRepository.GetUserVehicleLicensePlatesAsync(userId);
                foreach (var licensePlate in licensePlates)
                {
                    // Desactivar EventVehicles asociados al vehículo
                    var eventVehicleIds = await _vehicleRepository.GetVehicleEventVehicleIdsAsync(licensePlate);
                    foreach (var eventVehicleId in eventVehicleIds)
                    {
                        await _eventVehicleRepository.ToggleStatusAsync(eventVehicleId);
                    }

                    // Desactivar el vehículo
                    await _vehicleRepository.ToggleStatusAsync(licensePlate);
                }

                // Desactivar eventos del usuario
                var eventIds = await _userRepository.GetUserEventIdsAsync(userId);
                foreach (var eventId in eventIds)
                {
                    // Desactivar EventVehicles asociados al evento
                    var eventVehicleIds = await _eventRepository.GetEventEventVehicleIdsAsync(eventId);
                    foreach (var eventVehicleId in eventVehicleIds)
                    {
                        await _eventVehicleRepository.ToggleStatusAsync(eventVehicleId);
                    }

                    // Desactivar el evento
                    await _eventRepository.ToggleStatusAsync(eventId);
                }
            }

            // Finalmente, cambiar el estado del usuario
            return await _userRepository.ToggleStatusAsync(userId);
        }
    }
}
