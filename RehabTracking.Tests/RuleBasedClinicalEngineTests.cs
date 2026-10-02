using RehabTracking.Web.Features.Healthcare.ExerciseCatalog;
using Xunit;

namespace RehabTracking.Tests;

public class RuleBasedClinicalEngineTests
{
    private readonly ExercisesController _controller;

    public RuleBasedClinicalEngineTests()
    {
        // Khởi tạo controller (không cần db cho endpoint rule-evaluate)
        _controller = new ExercisesController(null!, null!);
    }

    [Fact]
    public void PreWorkoutPain_HighVAS_ShouldStopWorkoutAndAlertDoctor()
    {
        // Arrange
        var request = new SmartRuleEvaluationRequest
        {
            PreWorkoutPainVAS = 8,
            PostWorkoutPainVAS = 8,
            CompletionRate = 0,
            RecommendedPainMax = 4
        };

        // Act
        var result = _controller.EvaluateRules(request);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartRuleEvaluationResponse>(okResult.Value);

        // Assert
        Assert.False(response.CanProceedWorkout);
        Assert.Equal("DangerStop", response.SafetyStatus);
        Assert.True(response.TriggerDoctorAlert);
        Assert.Equal(0.0, response.LoadAdjustmentMultiplier);
        Assert.Contains("KHÔNG tập", response.ClinicalRecommendation);
    }

    [Fact]
    public void PreWorkoutPain_ModerateVAS_ShouldReduceLoadBy30Percent()
    {
        // Arrange
        var request = new SmartRuleEvaluationRequest
        {
            PreWorkoutPainVAS = 5,
            PostWorkoutPainVAS = 5,
            CompletionRate = 50,
            RecommendedPainMax = 4
        };

        // Act
        var result = _controller.EvaluateRules(request);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartRuleEvaluationResponse>(okResult.Value);

        // Assert
        Assert.True(response.CanProceedWorkout);
        Assert.Equal("Warning", response.SafetyStatus);
        Assert.Equal(0.7, response.LoadAdjustmentMultiplier);
        Assert.Contains("giảm 30% cường độ", response.ClinicalRecommendation);
    }

    [Fact]
    public void PostWorkoutPain_HighPainDelta_ShouldTriggerWarning()
    {
        // Arrange
        var request = new SmartRuleEvaluationRequest
        {
            PreWorkoutPainVAS = 2,
            PostWorkoutPainVAS = 6, // Tăng 4 điểm
            CompletionRate = 100,
            RecommendedPainMax = 4
        };

        // Act
        var result = _controller.EvaluateRules(request);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartRuleEvaluationResponse>(okResult.Value);

        // Assert
        Assert.Equal("Warning", response.SafetyStatus);
        Assert.Equal(0.75, response.LoadAdjustmentMultiplier);
        Assert.Contains("Cơn đau tăng đáng kể", response.ClinicalRecommendation);
    }

    [Fact]
    public void PostWorkout_ExcellentCompletionAndLowPain_ShouldRecommendAdvancement()
    {
        // Arrange
        var request = new SmartRuleEvaluationRequest
        {
            PreWorkoutPainVAS = 1,
            PostWorkoutPainVAS = 1,
            CompletionRate = 100,
            RecommendedPainMax = 4
        };

        // Act
        var result = _controller.EvaluateRules(request);
        var okResult = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
        var response = Assert.IsType<SmartRuleEvaluationResponse>(okResult.Value);

        // Assert
        Assert.Equal("Normal", response.SafetyStatus);
        Assert.Equal(1.1, response.LoadAdjustmentMultiplier);
        Assert.Contains("Thích ứng vận động tuyệt vời", response.ClinicalRecommendation);
    }
}
