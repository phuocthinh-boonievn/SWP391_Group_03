﻿using Business_Layer.AutoMapper;
using Business_Layer.DataAccess;
using Business_Layer.Repositories;
using Data_Layer.Models;
using Data_Layer.ResourceModel.ViewModel.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace API
{
    public class Startup
    {
        private IConfiguration Configuration;

        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen();
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", build => build.AllowAnyMethod()
                .AllowAnyHeader().AllowCredentials().SetIsOriginAllowed(hostName => true).Build());
            });


            services.AddAutoMapper(typeof(ApplicationMapper));

            InjectServices(services);
            ConfigureJWT(services);

        }

        //add services
        private void InjectServices(IServiceCollection services)
        {
            //var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            //string ConnectionStr = config.GetConnectionString("Db");
            //services.AddDbContext<EShopDBContext>(option => option.UseSqlServer(ConnectionStr));

            services.AddDbContext<FastFoodDeliveryDBContext>(options =>
            {
                options.UseSqlServer(Configuration.GetConnectionString("DB"));
            });
            // Add services repository pattern
            services.AddTransient<IMenuFoodItemRepository, MenuItemFoodRepository>();
            services.AddTransient<ICategoryRepository, CategoryRepository>();
            services.AddTransient<IDataService, RoleDataRepository>();
            services.AddTransient<IUserRepository, UserRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Lấy những services mà mình muốn, xét xem có role chưa nếu chưa có thì add vào
            var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider.GetServices<IDataService>();

            foreach (var service in services)
            {
                service.AddData().GetAwaiter().GetResult();
            }
            app.Run();
        }

        private void ConfigureJWT(IServiceCollection services)
        {
            services.Configure<JWTSetting>(Configuration.GetSection("JwtSetting"));
            services.Configure<AdminAccount>(Configuration.GetSection("AdminAccount"));
            services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<FastFoodDeliveryDBContext>()
                    .AddDefaultTokenProviders();
            services.AddIdentityCore<User>();

            services.Configure<IdentityOptions>(options =>
            {
                // setting for password
                // ví dụ như password có số, có chữ hoa, or có chữ thường hay không?
                // độ dài của password.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;

                // Setting for logout
                // Thời gian vào hệ thống
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                // Định nghĩa số lần nhập sai Password, or userName
                options.Lockout.MaxFailedAccessAttempts = 3;

                options.Lockout.AllowedForNewUsers = true;

                // Setting for user
                // Không được đăng kí trùng Email
                options.User.RequireUniqueEmail = true;
            });

            // Kiểm Tra xem mã Token có mapping được không
            // Kiểm tra JWTSetting
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["JWTSetting:Issuer"],
                    ValidAudience = Configuration["JWTSetting:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["JwtSetting:Key"]))
                };

            });
        }
    }
}