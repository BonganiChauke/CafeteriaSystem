using Microsoft.EntityFrameworkCore;
using CafeteriaSystem.Data;
using CafeteriaSystem.Services;
using Microsoft.AspNetCore.Identity;
using CafeteriaSystem.Models;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "CafeteriaAuthCookie";
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
   
});

builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IRestaurantService, RestaurantService>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); 
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedRolesAndUsersAsync(roleManager, userManager, context);
}

app.Run();

async Task SeedRolesAndUsersAsync(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager, ApplicationDbContext context)
{
    // Seed roles
    string[] roleNames = { "Admin", "Employee" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!roleResult.Succeeded)
            {
                Console.WriteLine($"Failed to create role {roleName}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
            }
        }
    }

    // Seed admin user
    var adminEmail = "admin@cafeteria.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail };
        var result = await userManager.CreateAsync(adminUser, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            context.Employees.Add(new Employee
            {
                Name = "Admin User",
                EmployeeNumber = "ADMIN001",
                Balance = 0,
                LastDepositMonth = DateTime.Now,
                UserId = adminUser.Id
            });
            await context.SaveChangesAsync();
            Console.WriteLine("Admin user created successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    // Seed default employee user
    var employeeEmail = "employee@cafeteria.com";
    var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
    if (employeeUser == null)
    {
        employeeUser = new IdentityUser { UserName = employeeEmail, Email = employeeEmail };
        var result = await userManager.CreateAsync(employeeUser, "Employee123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(employeeUser, "Employee");
            context.Employees.Add(new Employee
            {
                Name = "Default Employee",
                EmployeeNumber = "EMP001",
                Balance = 0,
                LastDepositMonth = DateTime.Now,
                UserId = employeeUser.Id
            });
            await context.SaveChangesAsync();
            Console.WriteLine("Default employee user created successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to create employee user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}