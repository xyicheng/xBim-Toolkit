#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    GlobalSuppressions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Diagnostics.CodeAnalysis;

#endregion

[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "Xbim.Ifc.PresentationAppearanceResource")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "Xbim.Ifc.PresentationOrganizationResource")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "Xbim.Ifc.StructuralAnalysisDomain")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "Xbim.Ifc.TimeSeriesResource")]
[assembly:
    SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace",
        Target = "Xbim.Ifc.Model")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "ComissioningEngineer"
        , Scope = "member", Target = "Xbim.Ifc.ActorResource.RoleEnum.#ComissioningEngineer")]
[assembly:
    SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Comissioning",
        Scope = "member", Target = "Xbim.Ifc.ActorResource.Role.#ComissioningEngineer")]