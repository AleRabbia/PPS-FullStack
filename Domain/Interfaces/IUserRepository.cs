﻿using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IUserRepository : IRepositoryBase<User>
    {
        User? GetUserByEmail(string? email);
        Task<bool> ExistsByIdentificationNumberAsync(string identificationNumber);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool>IsAdmin(int userId);
    }
}
