using Microsoft.EntityFrameworkCore;
using SensibleGovernment.Models;
using BCrypt.Net;

namespace SensibleGovernment.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if we already have data
        if (await context.Users.AnyAsync())
            return;

        // Create admin user with proper password hash
        var adminUser = new User
        {
            UserName = "Admin",
            Email = "admin@centralnews.com",
            // Hash the password "Admin123!" - you should change this!
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
            IsAdmin = true,
            IsActive = true,
            EmailConfirmed = true, // Set as confirmed for admin
            Created = DateTime.Now
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        // Create a sample regular user
        var sampleUser = new User
        {
            UserName = "JohnDoe",
            Email = "john@example.com",
            // Hash the password "Password123!" - you should change this!
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", BCrypt.Net.BCrypt.GenerateSalt(12)),
            IsAdmin = false,
            IsActive = true,
            EmailConfirmed = true, // Set as confirmed for testing
            Created = DateTime.Now
        };

        context.Users.Add(sampleUser);
        await context.SaveChangesAsync();

        // Create sample posts
        var posts = new List<Post>
        {
            new Post
            {
                Title = "Government Proposes New Digital Services Tax on Tech Giants",
                Content = @"The government has unveiled plans for a new 3% Digital Services Tax targeting large technology companies with global revenues exceeding £500 million. The proposed legislation, announced today by the Chancellor, aims to ensure that multinational tech corporations pay their 'fair share' of tax on UK-generated revenues.

The tax would apply to revenues generated from UK users through digital advertising, online marketplaces, and social media platforms. Officials estimate the measure could raise approximately £2 billion annually for public services.

Key provisions of the proposed tax include:
• A 3% levy on UK digital revenues
• Applicable to companies with global revenues above £500 million
• Exemption for financial services and online sales of own goods
• Implementation targeted for April 2025

Major tech companies including Google, Amazon, Facebook, and Apple would likely be affected by the new measures. The Treasury has emphasized that the tax is designed to be temporary, pending international agreement on digital taxation through the OECD.",

                Opinion = @"This Digital Services Tax represents a much-needed step towards tax fairness in the digital age. For too long, tech giants have exploited loopholes to minimize their tax contributions while traditional businesses shoulder a disproportionate burden.

The 3% rate strikes a reasonable balance - high enough to generate meaningful revenue without driving companies away. The £500 million threshold wisely protects smaller innovative companies from being caught in the net.

However, there are legitimate concerns about potential retaliation from the US government and the possibility that costs will simply be passed on to UK businesses who advertise on these platforms. The 'temporary' nature of the tax is also questionable - temporary taxes have a habit of becoming permanent fixtures.

Overall, this is a positive move that addresses public frustration about tax avoidance, but success will depend on implementation details and international cooperation. The government should remain flexible and ready to adjust if unintended consequences emerge.",

                FeaturedImageUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=1200&h=600&fit=crop",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1460925895917-afdab827c52f?w=400&h=300&fit=crop",
                ImageCaption = "Digital services and technology taxation",
                Topic = "News",
                AuthorId = adminUser.Id,
                Created = DateTime.Now.AddDays(-2),
                IsPublished = true,
                IsFeatured = true,
                ViewCount = 342,
                Sources = new List<PostSource>
                {
                    new PostSource
                    {
                        Title = "HM Treasury Official Announcement",
                        Url = "https://www.gov.uk/treasury/digital-services-tax",
                        Description = "Official government announcement and consultation document"
                    },
                    new PostSource
                    {
                        Title = "BBC News Coverage",
                        Url = "https://www.bbc.co.uk/news/business-digital-tax",
                        Description = "Comprehensive coverage including industry reaction"
                    },
                    new PostSource
                    {
                        Title = "Financial Times Analysis",
                        Url = "https://www.ft.com/content/digital-services-tax-uk",
                        Description = "In-depth financial analysis and international comparisons"
                    }
                }
            },

            new Post
            {
                Title = "NHS Announces Major Mental Health Initiative with £500M Funding",
                Content = @"The NHS has launched its most comprehensive mental health support programme to date, backed by £500 million in new funding. The initiative, announced by the Health Secretary this morning, aims to reduce waiting times and expand access to mental health services across England.

The funding will be allocated as follows:
• £200 million for 50 new mental health hubs in communities
• £150 million for children and young people's services
• £100 million for crisis intervention teams
• £50 million for digital mental health platforms

The programme promises to create 8,000 new mental health positions, including therapists, counselors, and support workers. The NHS expects to treat an additional 400,000 patients annually once fully operational.

Mental health charities have welcomed the announcement, though some express concerns about whether the funding is sufficient given the scale of need. Current statistics show that 1 in 4 adults experience mental health issues each year, with waiting times for treatment averaging 18 weeks in some regions.",

                Opinion = @"This mental health initiative is absolutely crucial and long overdue. The pandemic exposed and exacerbated a mental health crisis that was already reaching breaking point. The £500 million investment, while substantial, is really just the beginning of what's needed.

The focus on community hubs is particularly smart - bringing services closer to where people live removes barriers to access. Similarly, the emphasis on children's services could help prevent problems from escalating into adulthood.

My main concern is workforce recruitment. Creating 8,000 positions is meaningless if we can't fill them with qualified professionals. The government needs to simultaneously invest in training programmes and improve working conditions to retain staff.

The digital platforms component worries me slightly - while technology can expand reach, it shouldn't become a cheap substitute for face-to-face care. Human connection is often central to mental health recovery.

This initiative deserves support, but we must hold the government accountable for delivery and be prepared to demand more investment if these measures prove insufficient.",

                FeaturedImageUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=1200&h=600&fit=crop",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1576091160399-112ba8d25d1d?w=400&h=300&fit=crop",
                ImageCaption = "Mental health support and NHS services",
                Topic = "Health",
                AuthorId = adminUser.Id,
                Created = DateTime.Now.AddDays(-5),
                IsPublished = true,
                IsFeatured = false,
                ViewCount = 523,
                Sources = new List<PostSource>
                {
                    new PostSource
                    {
                        Title = "NHS England Official Statement",
                        Url = "https://www.england.nhs.uk/mental-health-investment",
                        Description = "Full details of the mental health investment programme"
                    },
                    new PostSource
                    {
                        Title = "Mind Charity Response",
                        Url = "https://www.mind.org.uk/news/nhs-funding-response",
                        Description = "Leading mental health charity's analysis"
                    },
                    new PostSource
                    {
                        Title = "The Guardian Health Section",
                        Url = "https://www.theguardian.com/society/mental-health-funding",
                        Description = "Investigation into mental health service gaps"
                    }
                }
            },

            new Post
            {
                Title = "Premier League Introduces Salary Cap Following Financial Fair Play Review",
                Content = @"The Premier League has voted to introduce a salary cap system starting from the 2025/26 season, marking the most significant financial reform in English football history. The decision, reached after months of deliberation, sees clubs agreeing to limit wage bills to 70% of revenue.

The new regulations include:
• Wage spending capped at 70% of club revenue
• Gradual implementation over three seasons
• Increased penalties for breaches including points deductions
• Enhanced monitoring and real-time financial reporting

Fourteen of the twenty Premier League clubs voted in favor, exceeding the required two-thirds majority. The 'Big Six' clubs initially resisted but eventually compromised after securing concessions on commercial revenue definitions.

UEFA has praised the move as aligning with European financial sustainability goals. However, the Professional Footballers' Association has expressed concerns about potential impacts on player welfare and career opportunities.",

                Opinion = @"The salary cap is a necessary evil that could save football from itself. The current arms race in wages is unsustainable - we've seen multiple clubs face financial crisis despite the Premier League's enormous revenues.

Critics will argue this limits ambition and could reduce the Premier League's ability to attract top talent. There's some truth to that, but the league's global appeal goes beyond just star players. The competitive balance and unpredictability that a salary cap could bring might actually enhance the product.

My biggest concern is enforcement. We've seen how creatively some clubs interpret Financial Fair Play rules. Without robust monitoring and meaningful penalties, this could become another paper tiger.

The 70% threshold seems reasonable - it allows well-run clubs to invest while preventing reckless spending. The gradual implementation is sensible too, giving clubs time to adjust contracts and business models.

If properly implemented, this could create a more sustainable and competitive league. But football has a way of finding loopholes, so vigilance will be key.",

                FeaturedImageUrl = "https://images.unsplash.com/photo-1489944440615-453fc2b6a9a9?w=1200&h=600&fit=crop",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1489944440615-453fc2b6a9a9?w=400&h=300&fit=crop",
                ImageCaption = "Premier League football and financial regulations",
                Topic = "Sport",
                AuthorId = adminUser.Id,
                Created = DateTime.Now.AddDays(-1),
                IsPublished = true,
                IsFeatured = true,
                ViewCount = 892,
                Sources = new List<PostSource>
                {
                    new PostSource
                    {
                        Title = "Premier League Official Statement",
                        Url = "https://www.premierleague.com/news/salary-cap-announcement",
                        Description = "Official announcement and regulation details"
                    },
                    new PostSource
                    {
                        Title = "Sky Sports Analysis",
                        Url = "https://www.skysports.com/football/salary-cap-impact",
                        Description = "Expert analysis on implications for clubs"
                    }
                }
            },

            new Post
            {
                Title = "Revolutionary AI Teaching Assistant Transforms Education in UK Schools",
                Content = @"A groundbreaking pilot programme introducing AI-powered teaching assistants in 100 UK schools has shown remarkable results, with participating students showing a 32% improvement in learning outcomes. The Department for Education announced plans today to expand the programme nationwide by September 2025.

The AI assistants, developed in partnership with leading UK tech firms, provide personalized learning support, instant feedback, and 24/7 availability for homework help. The system adapts to individual learning styles and identifies knowledge gaps in real-time.

Key features of the programme include:
• Personalized learning paths for each student
• Real-time progress tracking for teachers and parents
• Support for students with special educational needs
• Multi-language capabilities for ESL students
• Integration with existing school management systems

Teachers' unions, initially skeptical, have largely embraced the technology after assurances that it will supplement, not replace, human educators. The AI handles routine tasks like grading and progress tracking, freeing teachers to focus on creative instruction and emotional support.",

                Opinion = @"This AI education initiative represents the most exciting development in teaching since the internet entered classrooms. The 32% improvement in outcomes speaks for itself - this technology works.

What impresses me most is the thoughtful implementation. Rather than replacing teachers, the AI enhances their capabilities. Teachers can finally escape the administrative burden that consumes so much of their time and energy.

The personalization aspect is revolutionary. Every child learns differently, and finally we have technology that can adapt to individual needs at scale. This could be particularly transformative for students with learning difficulties who often get left behind in traditional one-size-fits-all education.

However, we must remain vigilant about data privacy and the digital divide. Not all students have equal access to technology at home. We also need to ensure that human interaction and social skills development aren't sacrificed at the altar of efficiency.

Overall, this is a bold step into the future of education. If implemented thoughtfully, it could help level the playing field and unlock the potential in every child.",

                FeaturedImageUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=1200&h=600&fit=crop",
                ThumbnailImageUrl = "https://images.unsplash.com/photo-1509062522246-3755977927d7?w=400&h=300&fit=crop",
                ImageCaption = "AI technology in modern classroom education",
                Topic = "Education",
                AuthorId = adminUser.Id,
                Created = DateTime.Now.AddDays(-3),
                IsPublished = true,
                IsFeatured = false,
                ViewCount = 445,
                Sources = new List<PostSource>
                {
                    new PostSource
                    {
                        Title = "Department for Education Announcement",
                        Url = "https://www.gov.uk/government/news/ai-teaching-assistants",
                        Description = "Official programme details and rollout timeline"
                    },
                    new PostSource
                    {
                        Title = "Educational Technology Review",
                        Url = "https://edtechmagazine.com/k12/article/ai-teaching-uk",
                        Description = "Independent analysis of the pilot programme results"
                    }
                }
            }
        };

        context.Posts.AddRange(posts);
        await context.SaveChangesAsync();

        // Add some sample comments
        var comments = new List<Comment>
        {
            new Comment
            {
                Content = "About time tech companies paid their fair share! They've been getting away with this for too long.",
                PostId = posts[0].Id,
                AuthorId = sampleUser.Id,
                Created = DateTime.Now.AddHours(-5)
            },
            new Comment
            {
                Content = "Mental health support is so important. I hope this funding reaches those who need it most.",
                PostId = posts[1].Id,
                AuthorId = sampleUser.Id,
                Created = DateTime.Now.AddHours(-3)
            }
        };

        context.Comments.AddRange(comments);
        await context.SaveChangesAsync();
    }
}