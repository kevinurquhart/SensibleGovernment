using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Data;
using SensibleGovernment.Models;
using System.Security.Cryptography;
using System.Text;

namespace SensibleGovernment.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private User? _currentUser;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    public User? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public event Action? OnAuthStateChanged;

    public async Task<(bool Success, string Message)> RegisterAsync(string userName, string email, string password)
    {
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.UserName == userName);

        if (existingUser != null)
        {
            return (false, "User with this email or username already exists");
        }

        // Create new user (in production, you'd hash the password)
        var user = new User
        {
            UserName = userName,
            Email = email
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _currentUser = user;
        OnAuthStateChanged?.Invoke();
        return (true, "Registration successful");
    }

    public async Task<(bool Success, string Message)> LoginAsync(string email, string password)
    {
        // In production, you'd verify hashed password
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            return (false, "Invalid email or password");
        }

        if (!user.IsActive)
        {
            return (false, "Your account has been suspended. Please contact support.");
        }

        user.LastLogin = DateTime.Now;
        await _context.SaveChangesAsync();

        _currentUser = user;
        OnAuthStateChanged?.Invoke();
        return (true, "Login successful");
    }

    public void Logout()
    {
        _currentUser = null;
        OnAuthStateChanged?.Invoke();
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    // Helper method to hash passwords (simplified - use proper hashing in production)
    private string HashPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}