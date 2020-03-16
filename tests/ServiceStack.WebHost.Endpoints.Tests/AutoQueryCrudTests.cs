using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public abstract class RockstarBase
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    [Alias(nameof(Rockstar))]
    public class RockstarAuto : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
    }
    
    public class RockstarAutoGuid : RockstarBase
    {
        [AutoId]
        public Guid Id { get; set; }
    }
    
    public class RockstarAudit : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedInfo { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string ModifiedBy { get; set; }
        public string ModifiedInfo { get; set; }
    }

    public interface IAudit 
    {
        DateTime CreatedDate { get; set; }
        string CreatedBy { get; set; }
        string CreatedInfo { get; set; }
        DateTime ModifiedDate { get; set; }
        string ModifiedBy { get; set; }
        string ModifiedInfo { get; set; }
        DateTime? SoftDeletedDate { get; set; }
        string SoftDeletedBy { get; set; }
        string SoftDeletedInfo { get; set; }
    }

    public interface IAuditTenant : IAudit
    {
        int TenantId { get; set; }
    }

    public abstract class AuditBase : IAudit
    {
        public DateTime CreatedDate { get; set; }
        [Required]
        public string CreatedBy { get; set; }
        [Required]
        public string CreatedInfo { get; set; }

        public DateTime ModifiedDate { get; set; }
        [Required]
        public string ModifiedBy { get; set; }
        [Required]
        public string ModifiedInfo { get; set; }

        [Index] //Check if Deleted
        public DateTime? SoftDeletedDate { get; set; }
        public string SoftDeletedBy { get; set; }
        public string SoftDeletedInfo { get; set; }
    }
        
    public class RockstarAuditTenant : AuditBase
    {
        [Index]
        public int TenantId { get; set; }
        [AutoIncrement]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }

    public class RockstarVersion : RockstarBase
    {
        [AutoIncrement]
        public int Id { get; set; }
        public ulong RowVersion { get; set; }
    }
    
    public class CreateRockstar : RockstarBase, ICreateDb<RockstarAuto>, IReturn<CreateRockstarResponse>
    {
    }

    public class CreateRockstarResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarWithReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
    }
    public class CreateRockstarWithVoidReturn : RockstarBase, ICreateDb<RockstarAuto>, IReturnVoid
    {
    }

    public class CreateRockstarWithAutoGuid : RockstarBase, ICreateDb<RockstarAutoGuid>, IReturn<CreateRockstarWithReturnGuidResponse>
    {
    }

    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.CreatedDate),  Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.CreatedBy),    Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.CreatedInfo),  Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public class CreateRockstarAudit : RockstarBase, ICreateDb<RockstarAudit>, IReturn<RockstarWithIdResponse>
    {
    }

    [Authenticate]
    [AutoPopulate(nameof(IAudit.CreatedDate),  Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.CreatedBy),    Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.CreatedInfo),  Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class CreateAuditBase<Table,TResponse> : ICreateDb<Table>, IReturn<TResponse> {}

    [AutoPopulate(nameof(IAuditTenant.TenantId), Eval = "Request.Items.TenantId")]
    public abstract class CreateAuditTenantBase<Table,TResponse> : CreateAuditBase<Table,TResponse> {}

    [Authenticate]
    [AutoPopulate(nameof(IAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class UpdateAuditBase<Table,TResponse> : IUpdateDb<Table>, IReturn<TResponse> {}

    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class UpdateAuditTenantBase<Table,TResponse> : UpdateAuditBase<Table,TResponse> {}

    [Authenticate]
    [AutoPopulate(nameof(IAudit.SoftDeletedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(IAudit.SoftDeletedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(IAudit.SoftDeletedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public abstract class SoftDeleteAuditBase<Table,TResponse> : IUpdateDb<Table>, IReturn<TResponse> {}
    
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class SoftDeleteAuditTenantBase<Table,TResponse> : SoftDeleteAuditBase<Table,TResponse> {}
    
    [Authenticate]
    [AutoFilter(QueryTerm.Ensure, nameof(IAudit.SoftDeletedDate), Template = SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public abstract class QueryDbTenant<From, Into> : QueryDb<From, Into> {}

    public class CreateRockstarAuditTenant : CreateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class UpdateRockstarAuditTenant : UpdateAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class CreateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime? DateDied { get; set; }
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class UpdateRockstarAuditTenantGateway : IReturn<RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }
    
    public class AutoCrudGatewayServices : Service
    {
        public object Any(CreateRockstarAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<CreateRockstarAuditTenant>();
            var response = Gateway.Send(gatewayRequest);
            return response;
        }
        
        public async Task<object> Any(UpdateRockstarAuditTenantGateway request)
        {
            var gatewayRequest = request.ConvertTo<UpdateRockstarAuditTenant>();
            var response = await Gateway.SendAsync(gatewayRequest);
            return response;
        }
    }
    
    public class SoftDeleteAuditTenant : SoftDeleteAuditTenantBase<RockstarAuditTenant, RockstarWithIdAndResultResponse>
    {
        public int Id { get; set; }
    }

    [Authenticate]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public class RealDeleteAuditTenant : IDeleteDb<RockstarAuditTenant>, IReturn<RockstarWithIdAndCountResponse>
    {
        public int Id { get; set; }
        public int? Age { get; set; }
    }

    public class QueryRockstarAudit : QueryDbTenant<RockstarAuditTenant, RockstarAuto>
    {
        public int? Id { get; set; }
    }

    [QueryDb(QueryTerm.Or)]
    [AutoFilter(QueryTerm.Ensure, nameof(AuditBase.SoftDeletedDate), SqlTemplate.IsNull)]
    [AutoFilter(QueryTerm.Ensure, nameof(IAuditTenant.TenantId),  Eval = "Request.Items.TenantId")]
    public class QueryRockstarAuditSubOr : QueryDb<RockstarAuditTenant, RockstarAuto>
    {
        public string FirstNameStartsWith { get; set; }
        public int? AgeOlderThan { get; set; }
    }

    public class CreateRockstarVersion : RockstarBase, ICreateDb<RockstarVersion>, IReturn<RockstarWithIdAndRowVersionResponse>
    {
    }
    
    public class RockstarWithIdResponse
    {
        public int Id { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    public class RockstarWithIdAndCountResponse
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class RockstarWithIdAndRowVersionResponse
    {
        public int Id { get; set; }
        public uint RowVersion { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class RockstarWithIdAndResultResponse
    {
        public int Id { get; set; }
        public RockstarAuto Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
    
    public class CreateRockstarWithReturnGuidResponse
    {
        public Guid Id { get; set; }
        public RockstarAutoGuid Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class CreateRockstarAdhocNonDefaults : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoDefault(Value = global::ServiceStack.WebHost.Endpoints.Tests.LivingStatus.Dead)]
        public LivingStatus? LivingStatus { get; set; }
    }

    public class CreateRockstarAutoMap : ICreateDb<RockstarAuto>, IReturn<RockstarWithIdAndResultResponse>
    {
        [AutoMap(nameof(RockstarAuto.FirstName))]
        public string MapFirstName { get; set; }

        [AutoMap(nameof(RockstarAuto.LastName))]
        public string MapLastName { get; set; }
        
        [AutoMap(nameof(RockstarAuto.Age))]
        [AutoDefault(Value = 21)]
        public int? MapAge { get; set; }
        
        [AutoMap(nameof(RockstarAuto.DateOfBirth))]
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime MapDateOfBirth { get; set; }

        [AutoMap(nameof(RockstarAuto.DateDied))]
        [AutoDefault(Eval = "utcNow")]
        public DateTime? MapDateDied { get; set; }
        
        [AutoMap(nameof(RockstarAuto.LivingStatus))]
        [AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus? MapLivingStatus { get; set; }
    }

    public class UpdateRockstar : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    [Authenticate]
    [AutoPopulate(nameof(RockstarAudit.ModifiedDate), Eval = "utcNow")]
    [AutoPopulate(nameof(RockstarAudit.ModifiedBy),   Eval = "userAuthName")] //or userAuthId
    [AutoPopulate(nameof(RockstarAudit.ModifiedInfo), Eval = "`${userSession.DisplayName} (${userSession.City})`")]
    public class UpdateRockstarAudit : RockstarBase, IPatchDb<RockstarAudit>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public LivingStatus? LivingStatus { get; set; }
    }

    [Authenticate]
    public class DeleteRockstarAudit : IDeleteDb<RockstarAudit>, IReturn<RockstarWithIdAndCountResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateRockstarVersion : RockstarBase, IPatchDb<RockstarVersion>, IReturn<RockstarWithIdAndRowVersionResponse>
    {
        public int Id { get; set; }
        public ulong RowVersion { get; set; }
    }
    
    public class PatchRockstar : RockstarBase, IPatchDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    [AutoUpdate(AutoUpdateStyle.NonDefaults)]
    public class UpdateRockstarNonDefaults : RockstarBase, IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }

    public class UpdateRockstarAdhocNonDefaults : IUpdateDb<RockstarAuto>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults)]
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [AutoDefault(Value = 21)]
        public int? Age { get; set; }
        [AutoDefault(Expression = "date(2001,1,1)")]
        public DateTime DateOfBirth { get; set; }
        [AutoDefault(Eval = "utcNow")]
        public DateTime? DateDied { get; set; }
        [AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = LivingStatus.Dead)]
        public LivingStatus LivingStatus { get; set; }
    }
    
    public class DeleteRockstar : IDeleteDb<Rockstar>, IReturn<EmptyResponse>
    {
        public int Id { get; set; }
    }
    
    public class DeleteRockstarFilters : IDeleteDb<Rockstar>, IReturn<DeleteRockstarCountResponse>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }
    }

    public class DeleteRockstarCountResponse
    {
        public int Count { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public partial class AutoQueryCrudTests
    {
        private readonly ServiceStackHost appHost;
        public IServiceClient client;

        private static readonly int TotalRockstars = AutoQueryAppHost.SeedRockstars.Length;
        private static readonly int TotalAlbums = AutoQueryAppHost.SeedAlbums.Length;
        private const string TenantId = nameof(TenantId);

        partial void OnConfigure(AutoQueryAppHost host, Funq.Container container);
        
        public AutoQueryCrudTests()
        {
            appHost = new AutoQueryAppHost {
                    ConfigureFn = (host,container) => {
                        container.AddSingleton<IAuthRepository>(c =>
                            new InMemoryAuthRepository());

                        host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                            new IAuthProvider[] {
                                new CredentialsAuthProvider(host.AppSettings),
                            }));
                        
                        var authRepo = container.Resolve<IAuthRepository>();
                        authRepo.InitSchema();
                        
                        authRepo.CreateUserAuth(new UserAuth {
                            Id = 1,
                            Email = "admin@email.com", 
                            DisplayName = "Admin User",
                            City = "London",
                        }, "p@55wOrd");
                        
                        authRepo.CreateUserAuth(new UserAuth {
                            Id = 2,
                            UserName = "manager", 
                            DisplayName = "The Manager",
                            City = "Perth",
                        }, "p@55wOrd");
                        
                        host.GlobalRequestFilters.Add((req, res, dto) => {
                            var userSession = req.SessionAs<AuthUserSession>();
                            if (userSession.IsAuthenticated)
                            {
                                req.SetItem(TenantId, userSession.City switch {
                                    "London" => 10,
                                    "Perth"  => 10,
                                    _        => 20,
                                });
                            }
                        });
                        OnConfigure(host, container);
                    }
                }
                .Init()
                .Start(Config.ListeningOn);
            
            using var db = appHost.TryResolve<IDbConnectionFactory>().OpenDbConnection();
            db.CreateTable<RockstarAudit>();
            db.CreateTable<RockstarAuditTenant>();
            db.CreateTable<RockstarAutoGuid>();
            db.CreateTable<RockstarVersion>();

            AutoMapping.RegisterPopulator((Dictionary<string,object> target, CreateRockstarWithAutoGuid source) => {
                if (source.FirstName == "Created")
                {
                    target[nameof(source.LivingStatus)] = LivingStatus.Dead;
                }
            });
            
            client = new JsonServiceClient(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown() => appHost.Dispose();

        public List<Rockstar> Rockstars => AutoQueryAppHost.SeedRockstars.ToList();

        public List<PagingTest> PagingTests => AutoQueryAppHost.SeedPagingTest.ToList();

        [Test]
        public void Can_CreateRockstar()
        {
            var request = new CreateRockstar {
                FirstName = "Return",
                LastName = "Empty",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var response = client.Post(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "Empty");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Return"));
        }

        [Test]
        public void Can_CreateRockstarWithReturn()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Return",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,2,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.GreaterThan(0));
            var newRockstar = response.Result;
            Assert.That(newRockstar.LastName, Is.EqualTo("Result"));
        }
 
        [Test]
        public void Can_CreateRockstarWithVoidReturn()
        {
            var request = new CreateRockstarWithVoidReturn {
                FirstName = "Return",
                LastName = "Void",
                Age = 20,
                DateOfBirth = new DateTime(2001,3,1),
                LivingStatus = LivingStatus.Alive,
            };

            client.Post(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.Single<Rockstar>(x => x.LastName == "Void");
            Assert.That(newRockstar.FirstName, Is.EqualTo("Return"));
        }
 
        [Test]
        public void Can_CreateRockstarWithAutoGuid()
        {
            var request = new CreateRockstarWithAutoGuid {
                FirstName = "Create",
                LastName = "AutoId",
                Age = 20,
                DateOfBirth = new DateTime(2001,4,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.Not.Null);
            var newRockstar = response.Result;
            Assert.That(newRockstar.Id, Is.EqualTo(response.Id));
            Assert.That(newRockstar.LastName, Is.EqualTo("AutoId"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }

        [Test]
        public void Can_CreateRockstarWithAutoGuid_with_Custom_Mapping()
        {
            var request = new CreateRockstarWithAutoGuid {
                FirstName = "Created",
                LastName = "AutoId",
                Age = 20,
                DateOfBirth = new DateTime(2001,5,1),
                LivingStatus = LivingStatus.Alive,
            };

            var response = client.Post(request);

            Assert.That(response.Id, Is.Not.Null);
            var newRockstar = response.Result;
            Assert.That(newRockstar.Id, Is.EqualTo(response.Id));
            Assert.That(newRockstar.LastName, Is.EqualTo("AutoId"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead)); //overridden by RegisterPopulator
        }

        [Test]
        public void Can_UpdateRockstar()
        {
            var createResponse = client.Post(new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            });
            
            var request = new UpdateRockstar {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Put(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.Null);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }
 
        [Test]
        public void Can_PatchRockstar()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new PatchRockstar {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Patch(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(createRequest.Age));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
        }
 
        [Test]
        public void Can_UpdateRockstarNonDefaults()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new UpdateRockstarNonDefaults {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            var response = client.Put(request);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(createRequest.Age));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
        }
  
        [Test]
        public void Can_UpdateRockstarAdhocNonDefaults()
        {
            var createRequest = new CreateRockstarWithReturn {
                FirstName = "UpdateReturn",
                LastName = "Result",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = client.Post(createRequest);
            
            var request = new UpdateRockstarAdhocNonDefaults {
                Id = createResponse.Id, 
                LastName = "UpdateResult",
            };

            using (JsConfig.With(new Text.Config { AssumeUtc = true }))
            {
                var response = client.Put(request);
            }

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateResult"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName)); //[AutoUpdate(AutoUpdateStyle.NonDefaults)]
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth, Is.EqualTo(new DateTime(2001,1,1)));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoUpdate(AutoUpdateStyle.NonDefaults), AutoDefault(Value = LivingStatus.Dead)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
        }

        [Test]
        public void Does_throw_when_no_rows_updated()
        {
            try
            {
                client.Post(new UpdateRockstar {
                    Id = 100,
                    LastName = "UpdateRockstar",
                });
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
        }

        [Test]
        public void Can_Delete_CreateRockstarWithReturn()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Rockstar",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(request);

            using var db = appHost.GetDbConnection();

            var newRockstar = db.Single<Rockstar>(x => x.Id == createResponse.Id);
            Assert.That(newRockstar, Is.Not.Null);

            var response = client.Delete(new DeleteRockstar {
                Id = createResponse.Id
            });

            newRockstar = db.Single<Rockstar>(x => x.Id == createResponse.Id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public void Does_throw_for_Delete_without_filters()
        {
            var request = new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Rockstar",
                Age = 20,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            };
            
            var createResponse = client.Post(request);

            try
            {
                var response = client.Delete(new DeleteRockstar());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(NotSupportedException)));
            }
        }

        [Test]
        public void Can_delete_with_multiple_non_PrimaryKey_filters()
        {
            var requests = 5.Times(i => new CreateRockstarWithReturn {
                FirstName = "Delete",
                LastName = "Filter" + i,
                Age = 23,
                DateOfBirth = new DateTime(2001,1,1),
                LivingStatus = LivingStatus.Alive,
            });
            
            requests.Each(x => client.Post(x));

            try
            {
                client.Delete(new DeleteRockstarFilters());
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(NotSupportedException)));
            }

            using var db = appHost.GetDbConnection();

            var response = client.Delete(new DeleteRockstarFilters { Age = 23, LastName = "Filter1" });
            Assert.That(response.Count, Is.EqualTo(1));
            var remaining = db.Select<Rockstar>(x => x.Age == 23);
            Assert.That(remaining.Count, Is.EqualTo(5 - 1));

            response = client.Delete(new DeleteRockstarFilters { Age = 23 });
            Assert.That(response.Count, Is.EqualTo(4));
            remaining = db.Select<Rockstar>(x => x.Age == 23);
            Assert.That(remaining.Count, Is.EqualTo(0));
        }
        
        [Test]
        public void Can_CreateRockstarAdhocNonDefaults()
        {
            var createRequest = new CreateRockstarAdhocNonDefaults {
                FirstName = "Create",
                LastName = "Defaults",
            };

            using var jsScope = JsConfig.With(new Text.Config { AssumeUtc = true });
            var createResponse = client.Post(createRequest);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("Defaults"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth, Is.EqualTo(new DateTime(2001,1,1)));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoDefault(Value = global::ServiceStack.WebHost.Endpoints.Tests.LivingStatus.Dead)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead));
        }
        
        [Test]
        public void Can_CreateRockstarAutoMap()
        {
            var createRequest = new CreateRockstarAutoMap {
                MapFirstName = "Map",
                MapLastName = "Defaults",
                MapDateOfBirth = new DateTime(2002,2,2),
                MapLivingStatus = LivingStatus.Alive,
            };

            var createResponse = client.Post(createRequest);

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<Rockstar>(createResponse.Id);
            Assert.That(newRockstar.LastName, Is.EqualTo("Defaults"));
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.MapFirstName));
            Assert.That(newRockstar.Age, Is.EqualTo(21)); //[AutoDefault(Value = 21)]
            //[AutoDefault(Eval = "date(2001,1,1)")]
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(new DateTime(2002,2,2).Date));
            Assert.That(newRockstar.DateDied.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            //[AutoDefault(Value = LivingStatus.Alive)]
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
        }

        [Test]
        public void Can_CreateRockstarAudit()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });
 
            var createResponse = authClient.Post(new CreateRockstarAudit {
                FirstName = "Create",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            });
            
            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Create"));
            Assert.That(newRockstar.LastName, Is.EqualTo("Audit"));
            Assert.That(newRockstar.Age, Is.EqualTo(20));
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(new DateTime(2002,2,2).Date));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Dead));
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("Admin User (London)"));

            authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "manager",
                Password = "p@55wOrd",
                RememberMe = true,
            });
 
            authClient.Patch(new UpdateRockstarAudit {
                Id = createResponse.Id,
                FirstName = "Updated",
                LivingStatus = LivingStatus.Alive,
            });

            newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Updated"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("The Manager (Perth)"));

            authClient.Delete(new DeleteRockstarAudit {
                Id = createResponse.Id,
            });

            newRockstar = db.SingleById<RockstarAudit>(createResponse.Id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public void Can_CreateRockstarAuditTenant()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var createRequest = new CreateRockstarAuditTenant {
                FirstName = "Create",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };
            var createResponse = authClient.Post(createRequest);
            Assert.That(createResponse.Id, Is.GreaterThan(0));
            var result = createResponse.Result;
            
            Assert.That(result.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
            
            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<RockstarAuditTenant>(createResponse.Id);
            Assert.That(newRockstar.TenantId, Is.EqualTo(10)); //admin.City London => 10
            Assert.That(newRockstar.FirstName, Is.EqualTo(createRequest.FirstName));
            Assert.That(newRockstar.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(newRockstar.Age, Is.EqualTo(createRequest.Age));
            Assert.That(newRockstar.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(createRequest.LivingStatus));
            
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("Admin User (London)"));

            Assert.That(authClient.Get(new QueryRockstarAudit { Id = createResponse.Id }).Results.Count,
                Is.EqualTo(1));

            authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "manager",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var updateRequest = new UpdateRockstarAuditTenant {
                Id = createResponse.Id,
                FirstName = "Updated",
                LivingStatus = LivingStatus.Alive,
            };
            var updateResponse = authClient.Patch(updateRequest);

            void assertUpdated(RockstarAuto result)
            {
                Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
                Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
                Assert.That(result.Age, Is.EqualTo(createRequest.Age));
                Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
                Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));
            }
            
            Assert.That(updateResponse.Id, Is.EqualTo(createResponse.Id));
            assertUpdated(updateResponse.Result);

            newRockstar = db.SingleById<RockstarAuditTenant>(createResponse.Id);
            Assert.That(newRockstar.FirstName, Is.EqualTo("Updated"));
            Assert.That(newRockstar.LivingStatus, Is.EqualTo(LivingStatus.Alive));
            
            Assert.That(newRockstar.CreatedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.CreatedBy, Is.EqualTo("admin@email.com"));
            Assert.That(newRockstar.CreatedInfo, Is.EqualTo("Admin User (London)"));
            Assert.That(newRockstar.ModifiedDate.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.ModifiedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.ModifiedInfo, Is.EqualTo("The Manager (Perth)"));
            
            Assert.That(authClient.Get(new QueryRockstarAuditSubOr {
                    FirstNameStartsWith = "Up",
                    AgeOlderThan = 18,
                }).Results.Count,
                Is.EqualTo(1));

            var softDeleteResponse = authClient.Delete(new SoftDeleteAuditTenant {
                Id = createResponse.Id,
            });

            Assert.That(softDeleteResponse.Id, Is.EqualTo(createResponse.Id));
            assertUpdated(softDeleteResponse.Result);

            newRockstar = db.SingleById<RockstarAuditTenant>(createResponse.Id);
            Assert.That(newRockstar.SoftDeletedDate.Value.Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(newRockstar.SoftDeletedBy, Is.EqualTo("manager"));
            Assert.That(newRockstar.SoftDeletedInfo, Is.EqualTo("The Manager (Perth)"));
            
            Assert.That(authClient.Get(new QueryRockstarAudit { Id = createResponse.Id }).Results.Count,
                Is.EqualTo(0));
            
            Assert.That(authClient.Get(new QueryRockstarAuditSubOr {
                    FirstNameStartsWith = "Up",
                    AgeOlderThan = 18,
                }).Results.Count,
                Is.EqualTo(0));

            var realDeleteResponse = authClient.Delete(new RealDeleteAuditTenant {
                Id = createResponse.Id,
                Age = 99 //non matching filter
            });
            Assert.That(realDeleteResponse.Id, Is.EqualTo(createResponse.Id));
            Assert.That(realDeleteResponse.Count, Is.EqualTo(0));
            newRockstar = db.SingleById<RockstarAuditTenant>(createResponse.Id);
            Assert.That(newRockstar, Is.Not.Null);

            realDeleteResponse = authClient.Delete(new RealDeleteAuditTenant {
                Id = createResponse.Id,
            });
            Assert.That(realDeleteResponse.Id, Is.EqualTo(createResponse.Id));
            Assert.That(realDeleteResponse.Count, Is.EqualTo(1));
            newRockstar = db.SingleById<RockstarAuditTenant>(createResponse.Id);
            Assert.That(newRockstar, Is.Null);
        }

        [Test]
        public void Can_CreateRockstarAuditTenantGateway_Gateway()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            authClient.Post(new Authenticate {
                provider = "credentials",
                UserName = "admin@email.com",
                Password = "p@55wOrd",
                RememberMe = true,
            });

            var createRequest = new CreateRockstarAuditTenantGateway {
                FirstName = "CreateGateway",
                LastName = "Audit",
                Age = 20,
                DateOfBirth = new DateTime(2002,2,2),
                LivingStatus = LivingStatus.Dead,
            };

            var createResponse = authClient.Post(createRequest);
            Assert.That(createResponse.Id, Is.GreaterThan(0));
            var result = createResponse.Result;

            var updateRequest = new UpdateRockstarAuditTenantGateway {
                Id = createResponse.Id,
                FirstName = "UpdatedGateway",
                LivingStatus = LivingStatus.Alive,
            };
            var updateResponse = authClient.Patch(updateRequest);
            result = updateResponse.Result;
            
            Assert.That(updateResponse.Id, Is.EqualTo(createResponse.Id));
            Assert.That(result.FirstName, Is.EqualTo(updateRequest.FirstName));
            Assert.That(result.LastName, Is.EqualTo(createRequest.LastName));
            Assert.That(result.Age, Is.EqualTo(createRequest.Age));
            Assert.That(result.DateOfBirth.Date, Is.EqualTo(createRequest.DateOfBirth.Date));
            Assert.That(result.LivingStatus, Is.EqualTo(updateRequest.LivingStatus));
        }
 
        [Test]
        public void Can_UpdateRockstarVersion()
        {
            var createResponse = client.Post(new CreateRockstarVersion {
                FirstName = "Create",
                LastName = "Version",
                Age = 20,
                DateOfBirth = new DateTime(2001,7,1),
                LivingStatus = LivingStatus.Dead,
            });

            try 
            {
                client.Patch(new UpdateRockstarVersion {
                    Id = createResponse.Id, 
                    LastName = "UpdateVersion2",
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
            
            var response = client.Patch(new UpdateRockstarVersion {
                Id = createResponse.Id, 
                LastName = "UpdateVersion3",
                RowVersion = createResponse.RowVersion,
            });

            using var db = appHost.GetDbConnection();
            var newRockstar = db.SingleById<RockstarVersion>(createResponse.Id);
            Assert.That(newRockstar.RowVersion, Is.Not.EqualTo(default(uint)));
            Assert.That(newRockstar.FirstName, Is.EqualTo("Create"));
            Assert.That(newRockstar.LastName, Is.EqualTo("UpdateVersion3"));

            try 
            {
                client.Patch(new UpdateRockstarVersion {
                    Id = createResponse.Id, 
                    LastName = "UpdateVersion4",
                });
                
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(OptimisticConcurrencyException)));
            }
        }
    }
}