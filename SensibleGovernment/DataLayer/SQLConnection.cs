using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;

namespace SensibleGovernment.DataLayer;

public class SQLConnection
{
    private readonly string _connectionString;
    private readonly ILogger<SQLConnection> _logger;

    public SQLConnection(IConfiguration configuration, ILogger<SQLConnection> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    /// <summary>
    /// Execute a stored procedure and return a DataTable
    /// </summary>
    public async Task<DataTable> ExecuteDataTableAsync(string storedProcName, SqlParameter[]? parameters = null)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(command);
            var dataTable = new DataTable();

            await connection.OpenAsync();
            adapter.Fill(dataTable);

            return dataTable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {StoredProc}", storedProcName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure and return a DataSet (for multiple result sets)
    /// </summary>
    public async Task<DataSet> ExecuteDataSetAsync(string storedProcName, SqlParameter[]? parameters = null)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            using var adapter = new SqlDataAdapter(command);
            var dataSet = new DataSet();

            await connection.OpenAsync();
            adapter.Fill(dataSet);

            return dataSet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {StoredProc}", storedProcName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure and return a single value
    /// </summary>
    public async Task<T?> ExecuteScalarAsync<T>(string storedProcName, SqlParameter[]? parameters = null)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
                return default(T);

            return (T)Convert.ChangeType(result, typeof(T));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {StoredProc}", storedProcName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure that doesn't return data (INSERT, UPDATE, DELETE)
    /// </summary>
    public async Task<int> ExecuteNonQueryAsync(string storedProcName, SqlParameter[]? parameters = null)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            if (parameters != null)
            {
                command.Parameters.AddRange(parameters);
            }

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {StoredProc}", storedProcName);
            throw;
        }
    }

    /// <summary>
    /// Execute a stored procedure with output parameters
    /// </summary>
    public async Task<Dictionary<string, object?>> ExecuteWithOutputParametersAsync(
        string storedProcName,
        SqlParameter[] parameters)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(storedProcName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

            command.Parameters.AddRange(parameters);

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();

            var outputValues = new Dictionary<string, object?>();
            foreach (SqlParameter param in command.Parameters)
            {
                if (param.Direction == ParameterDirection.Output ||
                    param.Direction == ParameterDirection.ReturnValue)
                {
                    outputValues[param.ParameterName] = param.Value;
                }
            }

            return outputValues;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing stored procedure {StoredProc}", storedProcName);
            throw;
        }
    }

    /// <summary>
    /// Helper method to create SQL parameters
    /// </summary>
    public static SqlParameter CreateParameter(string name, object? value)
    {
        return new SqlParameter(name, value ?? DBNull.Value);
    }

    /// <summary>
    /// Helper method to create output parameters
    /// </summary>
    public static SqlParameter CreateOutputParameter(string name, SqlDbType type, int size = 0)
    {
        var param = new SqlParameter(name, type)
        {
            Direction = ParameterDirection.Output
        };

        if (size > 0)
            param.Size = size;

        return param;
    }
}