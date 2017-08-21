﻿using System;
using OneTrueError.Client.Config;
using OneTrueError.Client.ContextCollections;
using OneTrueError.Client.Contracts;
using OneTrueError.Client.Converters;
using OneTrueError.Client.Processor;
using OneTrueError.Client.Reporters;

namespace OneTrueError.Client
{
    /// <summary>
    ///     Main mother of all true things.
    /// </summary>
    public class OneTrue
    {
        private static readonly ExceptionProcessor _exceptionProcessor;

        static OneTrue()
        {
            _exceptionProcessor = new ExceptionProcessor(Configuration);
        }

        /// <summary>
        ///     Access configuration options
        /// </summary>
        public static OneTrueConfiguration Configuration { get; } = new OneTrueConfiguration();


        /// <summary>
        ///     Will generate a report without uploading it.
        /// </summary>
        /// <param name="exception">Exception that you want to get reported</param>
        /// <returns>Unique identifier for this report (generated using <see cref="ReportIdGenerator" />)</returns>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        /// <remarks>
        ///     <para>
        ///         A lot if context information is also included in the error report. You can configure the attached information
        ///         by
        ///         using <c>OneTrue.Configuration.ContextProviders.Add()</c>
        ///     </para>
        ///     <para>
        ///         All library exceptions are directed to the <c>OneTrue.ReportingFailed</c> event.
        ///         Subscribe on that event if you have trouble with reporting exceptions.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code>
        /// public ActionResult Activate(UserViewModel model)
        /// {
        /// 	if (!ModelState.IsValid)
        /// 		return View(model);
        /// 		
        /// 	try
        /// 	{
        /// 		var user = _repos.GetUser(model.Id);
        /// 		user.Activate(model.ActivationCode);
        /// 		_repos.Save(user);
        /// 		return RedirectToAction("Welcome");
        /// 	}
        /// 	catch (Exception exception)
        /// 	{
        /// 		OneTrue.Report(exception);
        /// 	}
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="UploadReport" />
        public static ErrorReportDTO GenerateReport(Exception exception)
        {
            return _exceptionProcessor.Build(exception);
        }

        /// <summary>
        ///     Will generate a report without uploading it.
        /// </summary>
        /// <param name="exception">Exception that you want to get reported</param>
        /// <param name="contextData">
        ///     Context specific information which would make it easier to reproduce and correct the
        ///     exception. See <see cref="ObjectToContextCollectionConverter" /> to understand what kind of information you can
        ///     attach.
        /// </param>
        /// <returns>Unique identifier for this report (generated using <see cref="ReportIdGenerator" />)</returns>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        /// <remarks>
        ///     <para>
        ///         A lot if context information is also included in the error report. You can configure the attached information
        ///         by
        ///         using <c>OneTrue.Configuration.ContextProviders.Add()</c>
        ///     </para>
        ///     <para>
        ///         All library exceptions are directed to the <c>OneTrue.ReportingFailed</c> event.
        ///         Subscribe on that event if you have trouble with reporting exceptions.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code>
        /// public ActionResult Activate(UserViewModel model)
        /// {
        /// 	if (!ModelState.IsValid)
        /// 		return View(model);
        /// 		
        /// 	try
        /// 	{
        /// 		var user = _repos.GetUser(model.Id);
        /// 		user.Activate(model.ActivationCode);
        /// 		_repos.Save(user);
        /// 		return RedirectToAction("Welcome");
        /// 	}
        /// 	catch (Exception exception)
        /// 	{
        /// 		OneTrue.Report(exception);
        /// 	}
        /// }
        /// </code>
        /// </example>
        /// <seealso cref="UploadReport" />
        public static ErrorReportDTO GenerateReport(Exception exception, object contextData)
        {
            return _exceptionProcessor.Build(exception, contextData);
        }

        /// <summary>
        ///     Generate an error report
        /// </summary>
        /// <param name="context">context</param>
        /// <returns>generated report</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ErrorReportDTO GenerateReport(IErrorReporterContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            return _exceptionProcessor.Process(context);
        }

        /// <summary>
        ///     A user have written information about what he/she did when the exception was thrown; or the user want to get
        ///     information about the bug fixing progress (i.e. want to know when the error is corrected).
        /// </summary>
        /// <param name="errorId">Id generated by this library. Returned when you invoke <see cref="Report(System.Exception)" />.</param>
        /// <param name="feedback">Information from the user.</param>
        public static void LeaveFeedback(string errorId, UserSuppliedInformation feedback)
        {
            if (errorId == null) throw new ArgumentNullException("errorId");
            if (feedback == null) throw new ArgumentNullException("feedback");

            Configuration.Uploaders.Upload(new FeedbackDTO
            {
                Description = feedback.Description,
                EmailAddress = feedback.EmailAddress,
                ReportId = errorId
            });
        }

        /// <summary>
        ///     Report an exception directly.
        /// </summary>
        /// <param name="exception">Exception that you want to get reported</param>
        /// <returns>Unique identifier for this report (generated using <see cref="ReportIdGenerator" />)</returns>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        /// <remarks>
        ///     <para>
        ///         A lot if context information is also included in the error report. You can configure the attached information
        ///         by
        ///         using <c>OneTrue.Configuration.ContextProviders.Add()</c>
        ///     </para>
        ///     <para>
        ///         All library exceptions are directed to the <c>OneTrue.ReportingFailed</c> event.
        ///         Subscribe on that event if you have trouble with reporting exceptions.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code>
        /// public ActionResult Activate(UserViewModel model)
        /// {
        /// 	if (!ModelState.IsValid)
        /// 		return View(model);
        /// 		
        /// 	try
        /// 	{
        /// 		var user = _repos.GetUser(model.Id);
        /// 		user.Activate(model.ActivationCode);
        /// 		_repos.Save(user);
        /// 		return RedirectToAction("Welcome");
        /// 	}
        /// 	catch (Exception exception)
        /// 	{
        /// 		OneTrue.Report(exception);
        /// 	}
        /// }
        /// </code>
        /// </example>
        public static void Report(Exception exception)
        {
            _exceptionProcessor.Process(exception);
        }

        /// <summary>
        ///     Report an exception directly.
        /// </summary>
        /// <param name="exception">Exception that you want to get reported</param>
        /// <param name="contextData">
        ///     Context specific information which would make it easier to reproduce and correct the
        ///     exception. See <see cref="ObjectToContextCollectionConverter" /> to understand what kind of information you can
        ///     attach.
        /// </param>
        /// <returns>Unique identifier for this report (generated using <see cref="ReportIdGenerator" />)</returns>
        /// <exception cref="System.ArgumentNullException">exception</exception>
        /// <remarks>
        ///     <para>
        ///         Context information will be collected and included in the error report. You can configure the attached
        ///         information
        ///         by
        ///         using <c>OneTrue.Configuration.ContextProviders</c>
        ///     </para>
        ///     <para>
        ///         All library exceptions are directed to the <c>OneTrue.ReportingFailed</c> event.
        ///         Subscribe on that event if you have trouble with reporting exceptions.
        ///     </para>
        /// </remarks>
        /// <example>
        ///     <code>
        /// public ActionResult Activate(UserViewModel model)
        /// {
        /// 	if (!ModelState.IsValid)
        /// 		return View(model);
        /// 		
        /// 	try
        /// 	{
        /// 		var user = _repos.GetUser(model.Id);
        /// 		user.Activate(model.ActivationCode);
        /// 		_repos.Save(user);
        /// 		return RedirectToAction("Welcome");
        /// 	}
        /// 	catch (Exception exception)
        /// 	{
        /// 		OneTrue.Report(exception, model);
        /// 	}
        /// }
        /// </code>
        /// </example>
        public static void Report(Exception exception, object contextData)
        {
            if (exception == null) throw new ArgumentNullException("exception");
            _exceptionProcessor.Process(exception, contextData);
        }

        /// <summary>
        ///     Upload an error report.
        /// </summary>
        /// <param name="dto">Typically generated by <see cref="GenerateReport(System.Exception)" />.</param>
        public static void UploadReport(ErrorReportDTO dto)
        {
            if (dto == null) throw new ArgumentNullException("dto");
            Configuration.Uploaders.Upload(dto);
        }
    }
}