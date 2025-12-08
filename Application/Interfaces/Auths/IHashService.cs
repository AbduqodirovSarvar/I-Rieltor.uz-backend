using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Auths
{
    public interface IHashService
    {
        string Hash(string password);
        bool Verify(string password, string hashedPassword);
    }
}
