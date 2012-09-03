#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcInstances.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions;
using Xbim.Common.Logging;
using Xbim.XbimExtensions.Transactions;

#endregion

namespace Xbim.IO
{
    public class IfcMetaProperty
    {
        public PropertyInfo PropertyInfo;
        public IfcAttribute IfcAttribute;
    }


   
  


    /// <summary>
    ///   A collection of IPersistIfcEntity instances, optimised for IFC models
    /// </summary>
    [Serializable]
    public class IfcInstances : ICollection<IPersistIfcEntity>, IEnumerable<long>
    {
        private readonly ILogger Logger = LoggerFactory.GetLogger();
        private readonly Dictionary<Type, ICollection<long>> _typeLookup = new Dictionary<Type, ICollection<long>>();
        private static Dictionary<ushort, string> _IfcIdTypeStringLookup = new Dictionary<ushort, string>();
        private static Dictionary<ushort, IfcType> _IfcIdIfcTypeLookup = new Dictionary<ushort, IfcType>();

        public static Dictionary<ushort, IfcType> IfcIdIfcTypeLookup
        {
            get { return IfcInstances._IfcIdIfcTypeLookup; }
        }
        private IfcInstanceKeyedCollection _entityHandleLookup = new IfcInstanceKeyedCollection();
        private  bool _buildIndices = true;
        private readonly IModel _model;
        private long _highestLabel;

        public long HighestLabel
        {
            get { return _highestLabel; }
            
        }

        private long NextLabel()
        {  
            return _highestLabel+1;
        }


        [NonSerialized] 
        private static IfcTypeDictionary _IfcEntities;

        public static IfcTypeDictionary IfcEntities
        {
            get { return _IfcEntities; }
            set { _IfcEntities = value; }
        }

        static IfcInstances()
        {
            Module ifcModule = typeof (IfcActor).Module;
            IEnumerable<Type> types =
                ifcModule.GetTypes().Where(
                    t =>
                    typeof (IPersistIfc).IsAssignableFrom(t) && t != typeof (IPersistIfc) && !t.IsEnum && !t.IsAbstract &&
                    t.IsPublic && !typeof (ExpressHeaderType).IsAssignableFrom(t));

            _IfcTypeLookup = new Dictionary<string, IfcType>(types.Count());
            _IfcEntities = new IfcTypeDictionary();
            
            InitIdTypeLookup();
            try
            {
                foreach (Type type in types)
                {
                    IfcType ifcType;

                    if (_IfcEntities.Contains(type))
                        ifcType = _IfcEntities[type];
                    else
                        ifcType = new IfcType {Type = type};

                    string typeLookup = type.Name.ToUpper();
                    if (!_IfcTypeLookup.ContainsKey(typeLookup))
                    {
                        _IfcTypeLookup.Add(typeLookup, ifcType);
                    }
                    if (!_IfcEntities.Contains(ifcType))
                    {
                        _IfcEntities.Add(ifcType);
                        AddParent(ifcType);
                        AddProperties(ifcType);
                    }
                }
                //add the Type Ids to each of the IfcTypes
                foreach (var item in _IfcIdTypeStringLookup)
                {
                    IfcType ifcType = _IfcTypeLookup[item.Value];
                    ifcType.TypeId = item.Key;
                    _IfcIdIfcTypeLookup.Add(_IfcTypeLookup[item.Value].TypeId, ifcType);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error reading Ifc Entity Meta Data", e);
            }
        }

        private static void InitIdTypeLookup()
        {
            IfcIdTypeStringLookup.Add(50, "IFC2DCOMPOSITECURVE");
            IfcIdTypeStringLookup.Add(100, "IFCACTOR");
            IfcIdTypeStringLookup.Add(150, "IFCACTORROLE");
            IfcIdTypeStringLookup.Add(200, "IFCACTUATORTYPE");
            IfcIdTypeStringLookup.Add(250, "IFCAIRTERMINALBOXTYPE");
            IfcIdTypeStringLookup.Add(300, "IFCAIRTERMINALTYPE");
            IfcIdTypeStringLookup.Add(350, "IFCAIRTOAIRHEATRECOVERYTYPE");
            IfcIdTypeStringLookup.Add(400, "IFCALARMTYPE");
            IfcIdTypeStringLookup.Add(450, "IFCANNOTATION");
            IfcIdTypeStringLookup.Add(500, "IFCANNOTATIONCURVEOCCURRENCE");
            IfcIdTypeStringLookup.Add(550, "IFCANNOTATIONFILLAREA");
            IfcIdTypeStringLookup.Add(600, "IFCANNOTATIONFILLAREAOCCURRENCE");
            IfcIdTypeStringLookup.Add(650, "IFCANNOTATIONSURFACEOCCURRENCE");
            IfcIdTypeStringLookup.Add(700, "IFCANNOTATIONSYMBOLOCCURRENCE");
            IfcIdTypeStringLookup.Add(750, "IFCANNOTATIONTEXTOCCURRENCE");
            IfcIdTypeStringLookup.Add(800, "IFCAPPLICATION");
            IfcIdTypeStringLookup.Add(850, "IFCAPPLIEDVALUERELATIONSHIP");
            IfcIdTypeStringLookup.Add(900, "IFCAPPROVAL");
            IfcIdTypeStringLookup.Add(950, "IFCAPPROVALACTORRELATIONSHIP");
            IfcIdTypeStringLookup.Add(1000, "IFCAPPROVALPROPERTYRELATIONSHIP");
            IfcIdTypeStringLookup.Add(1050, "IFCAPPROVALRELATIONSHIP");
            IfcIdTypeStringLookup.Add(1100, "IFCARBITRARYCLOSEDPROFILEDEF");
            IfcIdTypeStringLookup.Add(1150, "IFCARBITRARYOPENPROFILEDEF");
            IfcIdTypeStringLookup.Add(1200, "IFCARBITRARYPROFILEDEFWITHVOIDS");
            IfcIdTypeStringLookup.Add(1250, "IFCASYMMETRICISHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(1300, "IFCAXIS1PLACEMENT");
            IfcIdTypeStringLookup.Add(1350, "IFCAXIS2PLACEMENT2D");
            IfcIdTypeStringLookup.Add(1400, "IFCAXIS2PLACEMENT3D");
            IfcIdTypeStringLookup.Add(1450, "IFCBEAM");
            IfcIdTypeStringLookup.Add(1500, "IFCBEAMTYPE");
            IfcIdTypeStringLookup.Add(1550, "IFCBEZIERCURVE");
            IfcIdTypeStringLookup.Add(1600, "IFCBOILERTYPE");
            IfcIdTypeStringLookup.Add(1650, "IFCBOOLEANCLIPPINGRESULT");
            IfcIdTypeStringLookup.Add(1700, "IFCBOOLEANRESULT");
            IfcIdTypeStringLookup.Add(1750, "IFCBOUNDARYEDGECONDITION");
            IfcIdTypeStringLookup.Add(1800, "IFCBOUNDARYFACECONDITION");
            IfcIdTypeStringLookup.Add(1850, "IFCBOUNDARYNODECONDITION");
            IfcIdTypeStringLookup.Add(1900, "IFCBOUNDARYNODECONDITIONWARPING");
            IfcIdTypeStringLookup.Add(1950, "IFCBOUNDINGBOX");
            IfcIdTypeStringLookup.Add(2000, "IFCBOXEDHALFSPACE");
            IfcIdTypeStringLookup.Add(2050, "IFCBUILDING");
            IfcIdTypeStringLookup.Add(2100, "IFCBUILDINGELEMENTPART");
            IfcIdTypeStringLookup.Add(2150, "IFCBUILDINGELEMENTPROXY");
            IfcIdTypeStringLookup.Add(2200, "IFCBUILDINGELEMENTPROXYTYPE");
            IfcIdTypeStringLookup.Add(2250, "IFCBUILDINGSTOREY");
            IfcIdTypeStringLookup.Add(2300, "IFCCABLECARRIERFITTINGTYPE");
            IfcIdTypeStringLookup.Add(2350, "IFCCABLECARRIERSEGMENTTYPE");
            IfcIdTypeStringLookup.Add(2400, "IFCCABLESEGMENTTYPE");
            IfcIdTypeStringLookup.Add(2450, "IFCCALENDARDATE");
            IfcIdTypeStringLookup.Add(2500, "IFCCARTESIANPOINT");
            IfcIdTypeStringLookup.Add(2550, "IFCCARTESIANTRANSFORMATIONOPERATOR2D");
            IfcIdTypeStringLookup.Add(2600, "IFCCARTESIANTRANSFORMATIONOPERATOR2DNONUNIFORM");
            IfcIdTypeStringLookup.Add(2650, "IFCCARTESIANTRANSFORMATIONOPERATOR3D");
            IfcIdTypeStringLookup.Add(2700, "IFCCARTESIANTRANSFORMATIONOPERATOR3DNONUNIFORM");
            IfcIdTypeStringLookup.Add(2750, "IFCCENTERLINEPROFILEDEF");
            IfcIdTypeStringLookup.Add(2800, "IFCCHILLERTYPE");
            IfcIdTypeStringLookup.Add(2850, "IFCCIRCLE");
            IfcIdTypeStringLookup.Add(2900, "IFCCIRCLEHOLLOWPROFILEDEF");
            IfcIdTypeStringLookup.Add(2950, "IFCCIRCLEPROFILEDEF");
            IfcIdTypeStringLookup.Add(3000, "IFCCLASSIFICATION");
            IfcIdTypeStringLookup.Add(3050, "IFCCLASSIFICATIONITEM");
            IfcIdTypeStringLookup.Add(3100, "IFCCLASSIFICATIONITEMRELATIONSHIP");
            IfcIdTypeStringLookup.Add(3150, "IFCCLASSIFICATIONNOTATION");
            IfcIdTypeStringLookup.Add(3200, "IFCCLASSIFICATIONNOTATIONFACET");
            IfcIdTypeStringLookup.Add(3250, "IFCCLASSIFICATIONREFERENCE");
            IfcIdTypeStringLookup.Add(3300, "IFCCLOSEDSHELL");
            IfcIdTypeStringLookup.Add(3350, "IFCCOILTYPE");
            IfcIdTypeStringLookup.Add(3400, "IFCCOLOURRGB");
            IfcIdTypeStringLookup.Add(3450, "IFCCOLUMN");
            IfcIdTypeStringLookup.Add(3500, "IFCCOLUMNTYPE");
            IfcIdTypeStringLookup.Add(3550, "IFCCOMPLEXPROPERTY");
            IfcIdTypeStringLookup.Add(3600, "IFCCOMPOSITECURVE");
            IfcIdTypeStringLookup.Add(3650, "IFCCOMPOSITECURVESEGMENT");
            IfcIdTypeStringLookup.Add(3700, "IFCCOMPOSITEPROFILEDEF");
            IfcIdTypeStringLookup.Add(3750, "IFCCOMPRESSORTYPE");
            IfcIdTypeStringLookup.Add(3800, "IFCCONDENSERTYPE");
            IfcIdTypeStringLookup.Add(3850, "IFCCONNECTEDFACESET");
            IfcIdTypeStringLookup.Add(3900, "IFCCONNECTIONCURVEGEOMETRY");
            IfcIdTypeStringLookup.Add(3950, "IFCCONNECTIONPOINTGEOMETRY");
            IfcIdTypeStringLookup.Add(4000, "IFCCONNECTIONSURFACEGEOMETRY");
            IfcIdTypeStringLookup.Add(4050, "IFCCONSTRAINTAGGREGATIONRELATIONSHIP");
            IfcIdTypeStringLookup.Add(4100, "IFCCONSTRAINTCLASSIFICATIONRELATIONSHIP");
            IfcIdTypeStringLookup.Add(4150, "IFCCONSTRAINTRELATIONSHIP");
            IfcIdTypeStringLookup.Add(4200, "IFCCONSTRUCTIONEQUIPMENTRESOURCE");
            IfcIdTypeStringLookup.Add(4250, "IFCCONSTRUCTIONMATERIALRESOURCE");
            IfcIdTypeStringLookup.Add(4300, "IFCCONSTRUCTIONPRODUCTRESOURCE");
            IfcIdTypeStringLookup.Add(4350, "IFCCONTEXTDEPENDENTUNIT");
            IfcIdTypeStringLookup.Add(4400, "IFCCONTROLLERTYPE");
            IfcIdTypeStringLookup.Add(4450, "IFCCONVERSIONBASEDUNIT");
            IfcIdTypeStringLookup.Add(4500, "IFCCOOLEDBEAMTYPE");
            IfcIdTypeStringLookup.Add(4550, "IFCCOOLINGTOWERTYPE");
            IfcIdTypeStringLookup.Add(4600, "IFCCOORDINATEDUNIVERSALTIMEOFFSET");
            IfcIdTypeStringLookup.Add(4650, "IFCCOSTVALUE");
            IfcIdTypeStringLookup.Add(4700, "IFCCOVERING");
            IfcIdTypeStringLookup.Add(4750, "IFCCOVERINGTYPE");
            IfcIdTypeStringLookup.Add(4800, "IFCCRANERAILASHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(4850, "IFCCRANERAILFSHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(4900, "IFCCREWRESOURCE");
            IfcIdTypeStringLookup.Add(4950, "IFCCSGSOLID");
            IfcIdTypeStringLookup.Add(5000, "IFCCSHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(5050, "IFCCURRENCYRELATIONSHIP");
            IfcIdTypeStringLookup.Add(5100, "IFCCURTAINWALL");
            IfcIdTypeStringLookup.Add(5150, "IFCCURTAINWALLTYPE");
            IfcIdTypeStringLookup.Add(5200, "IFCCURVEBOUNDEDPLANE");
            IfcIdTypeStringLookup.Add(5250, "IFCCURVESTYLE");
            IfcIdTypeStringLookup.Add(5300, "IFCCURVESTYLEFONT");
            IfcIdTypeStringLookup.Add(5350, "IFCCURVESTYLEFONTANDSCALING");
            IfcIdTypeStringLookup.Add(5400, "IFCCURVESTYLEFONTPATTERN");
            IfcIdTypeStringLookup.Add(5450, "IFCDAMPERTYPE");
            IfcIdTypeStringLookup.Add(5500, "IFCDATEANDTIME");
            IfcIdTypeStringLookup.Add(5550, "IFCDEFINEDSYMBOL");
            IfcIdTypeStringLookup.Add(5600, "IFCDERIVEDPROFILEDEF");
            IfcIdTypeStringLookup.Add(5650, "IFCDERIVEDUNIT");
            IfcIdTypeStringLookup.Add(5700, "IFCDERIVEDUNITELEMENT");
            IfcIdTypeStringLookup.Add(5750, "IFCDIMENSIONALEXPONENTS");
            IfcIdTypeStringLookup.Add(5800, "IFCDIRECTION");
            IfcIdTypeStringLookup.Add(5850, "IFCDISCRETEACCESSORY");
            IfcIdTypeStringLookup.Add(5900, "IFCDISCRETEACCESSORYTYPE");
            IfcIdTypeStringLookup.Add(5950, "IFCDISTRIBUTIONCHAMBERELEMENT");
            IfcIdTypeStringLookup.Add(6000, "IFCDISTRIBUTIONCHAMBERELEMENTTYPE");
            IfcIdTypeStringLookup.Add(6050, "IFCDISTRIBUTIONCONTROLELEMENT");
            IfcIdTypeStringLookup.Add(6100, "IFCDISTRIBUTIONELEMENT");
            IfcIdTypeStringLookup.Add(6150, "IFCDISTRIBUTIONELEMENTTYPE");
            IfcIdTypeStringLookup.Add(6200, "IFCDISTRIBUTIONFLOWELEMENT");
            IfcIdTypeStringLookup.Add(6250, "IFCDISTRIBUTIONPORT");
            IfcIdTypeStringLookup.Add(6300, "IFCDOCUMENTELECTRONICFORMAT");
            IfcIdTypeStringLookup.Add(6350, "IFCDOCUMENTINFORMATION");
            IfcIdTypeStringLookup.Add(6400, "IFCDOCUMENTINFORMATIONRELATIONSHIP");
            IfcIdTypeStringLookup.Add(6450, "IFCDOCUMENTREFERENCE");
            IfcIdTypeStringLookup.Add(6500, "IFCDOOR");
            IfcIdTypeStringLookup.Add(6550, "IFCDOORLININGPROPERTIES");
            IfcIdTypeStringLookup.Add(6600, "IFCDOORPANELPROPERTIES");
            IfcIdTypeStringLookup.Add(6650, "IFCDOORSTYLE");
            IfcIdTypeStringLookup.Add(6700, "IFCDRAUGHTINGCALLOUT");
            IfcIdTypeStringLookup.Add(6750, "IFCDRAUGHTINGPREDEFINEDCOLOUR");
            IfcIdTypeStringLookup.Add(6800, "IFCDRAUGHTINGPREDEFINEDCURVEFONT");
            IfcIdTypeStringLookup.Add(6850, "IFCDUCTFITTINGTYPE");
            IfcIdTypeStringLookup.Add(6900, "IFCDUCTSEGMENTTYPE");
            IfcIdTypeStringLookup.Add(6950, "IFCDUCTSILENCERTYPE");
            IfcIdTypeStringLookup.Add(7000, "IFCEDGE");
            IfcIdTypeStringLookup.Add(7050, "IFCEDGECURVE");
            IfcIdTypeStringLookup.Add(7100, "IFCEDGELOOP");
            IfcIdTypeStringLookup.Add(7150, "IFCELECTRICALBASEPROPERTIES");
            IfcIdTypeStringLookup.Add(7200, "IFCELECTRICALCIRCUIT");
            IfcIdTypeStringLookup.Add(7250, "IFCELECTRICALELEMENT");
            IfcIdTypeStringLookup.Add(7300, "IFCELECTRICAPPLIANCETYPE");
            IfcIdTypeStringLookup.Add(7350, "IFCELECTRICDISTRIBUTIONPOINT");
            IfcIdTypeStringLookup.Add(7400, "IFCELECTRICFLOWSTORAGEDEVICETYPE");
            IfcIdTypeStringLookup.Add(7450, "IFCELECTRICGENERATORTYPE");
            IfcIdTypeStringLookup.Add(7500, "IFCELECTRICHEATERTYPE");
            IfcIdTypeStringLookup.Add(7550, "IFCELECTRICMOTORTYPE");
            IfcIdTypeStringLookup.Add(7600, "IFCELECTRICTIMECONTROLTYPE");
            IfcIdTypeStringLookup.Add(7650, "IFCELEMENTASSEMBLY");
            IfcIdTypeStringLookup.Add(7700, "IFCELEMENTQUANTITY");
            IfcIdTypeStringLookup.Add(7750, "IFCELLIPSE");
            IfcIdTypeStringLookup.Add(7800, "IFCELLIPSEPROFILEDEF");
            IfcIdTypeStringLookup.Add(7850, "IFCENERGYCONVERSIONDEVICE");
            IfcIdTypeStringLookup.Add(7900, "IFCENERGYPROPERTIES");
            IfcIdTypeStringLookup.Add(7950, "IFCENVIRONMENTALIMPACTVALUE");
            IfcIdTypeStringLookup.Add(8000, "IFCEQUIPMENTELEMENT");
            IfcIdTypeStringLookup.Add(8050, "IFCEVAPORATIVECOOLERTYPE");
            IfcIdTypeStringLookup.Add(8100, "IFCEVAPORATORTYPE");
            IfcIdTypeStringLookup.Add(8150, "IFCEXTENDEDMATERIALPROPERTIES");
            IfcIdTypeStringLookup.Add(8200, "IFCEXTERNALLYDEFINEDSURFACESTYLE");
            IfcIdTypeStringLookup.Add(8250, "IFCEXTERNALLYDEFINEDSYMBOL");
            IfcIdTypeStringLookup.Add(8300, "IFCEXTERNALLYDEFINEDTEXTFONT");
            IfcIdTypeStringLookup.Add(8350, "IFCEXTRUDEDAREASOLID");
            IfcIdTypeStringLookup.Add(8400, "IFCFACE");
            IfcIdTypeStringLookup.Add(8450, "IFCFACEBASEDSURFACEMODEL");
            IfcIdTypeStringLookup.Add(8500, "IFCFACEBOUND");
            IfcIdTypeStringLookup.Add(8550, "IFCFACEOUTERBOUND");
            IfcIdTypeStringLookup.Add(8600, "IFCFACESURFACE");
            IfcIdTypeStringLookup.Add(8650, "IFCFACETEDBREP");
            IfcIdTypeStringLookup.Add(8700, "IFCFACETEDBREPWITHVOIDS");
            IfcIdTypeStringLookup.Add(8750, "IFCFAILURECONNECTIONCONDITION");
            IfcIdTypeStringLookup.Add(8800, "IFCFANTYPE");
            IfcIdTypeStringLookup.Add(8850, "IFCFASTENER");
            IfcIdTypeStringLookup.Add(8900, "IFCFASTENERTYPE");
            IfcIdTypeStringLookup.Add(8950, "IFCFILLAREASTYLE");
            IfcIdTypeStringLookup.Add(9000, "IFCFILLAREASTYLEHATCHING");
            IfcIdTypeStringLookup.Add(9050, "IFCFILTERTYPE");
            IfcIdTypeStringLookup.Add(9100, "IFCFIRESUPPRESSIONTERMINALTYPE");
            IfcIdTypeStringLookup.Add(9150, "IFCFLOWCONTROLLER");
            IfcIdTypeStringLookup.Add(9200, "IFCFLOWFITTING");
            IfcIdTypeStringLookup.Add(9250, "IFCFLOWINSTRUMENTTYPE");
            IfcIdTypeStringLookup.Add(9300, "IFCFLOWMETERTYPE");
            IfcIdTypeStringLookup.Add(9350, "IFCFLOWMOVINGDEVICE");
            IfcIdTypeStringLookup.Add(9400, "IFCFLOWSEGMENT");
            IfcIdTypeStringLookup.Add(9450, "IFCFLOWSTORAGEDEVICE");
            IfcIdTypeStringLookup.Add(9500, "IFCFLOWTERMINAL");
            IfcIdTypeStringLookup.Add(9550, "IFCFLOWTREATMENTDEVICE");
            IfcIdTypeStringLookup.Add(9600, "IFCFLUIDFLOWPROPERTIES");
            IfcIdTypeStringLookup.Add(9650, "IFCFOOTING");
            IfcIdTypeStringLookup.Add(9700, "IFCFURNISHINGELEMENT");
            IfcIdTypeStringLookup.Add(9750, "IFCFURNISHINGELEMENTTYPE");
            IfcIdTypeStringLookup.Add(9800, "IFCFURNITURETYPE");
            IfcIdTypeStringLookup.Add(9850, "IFCGENERALPROFILEPROPERTIES");
            IfcIdTypeStringLookup.Add(9900, "IFCGEOMETRICCURVESET");
            IfcIdTypeStringLookup.Add(9950, "IFCGEOMETRICREPRESENTATIONCONTEXT");
            IfcIdTypeStringLookup.Add(10000, "IFCGEOMETRICREPRESENTATIONSUBCONTEXT");
            IfcIdTypeStringLookup.Add(10050, "IFCGEOMETRICSET");
            IfcIdTypeStringLookup.Add(10100, "IFCGRID");
            IfcIdTypeStringLookup.Add(10150, "IFCGRIDAXIS");
            IfcIdTypeStringLookup.Add(10200, "IFCGRIDPLACEMENT");
            IfcIdTypeStringLookup.Add(10250, "IFCGROUP");
            IfcIdTypeStringLookup.Add(10300, "IFCHALFSPACESOLID");
            IfcIdTypeStringLookup.Add(10350, "IFCHEATEXCHANGERTYPE");
            IfcIdTypeStringLookup.Add(10400, "IFCHUMIDIFIERTYPE");
            IfcIdTypeStringLookup.Add(10450, "IFCIRREGULARTIMESERIES");
            IfcIdTypeStringLookup.Add(10500, "IFCIRREGULARTIMESERIESVALUE");
            IfcIdTypeStringLookup.Add(10550, "IFCISHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(10600, "IFCJUNCTIONBOXTYPE");
            IfcIdTypeStringLookup.Add(10650, "IFCLABORRESOURCE");
            IfcIdTypeStringLookup.Add(10700, "IFCLAMPTYPE");
            IfcIdTypeStringLookup.Add(10750, "IFCLIBRARYINFORMATION");
            IfcIdTypeStringLookup.Add(10800, "IFCLIBRARYREFERENCE");
            IfcIdTypeStringLookup.Add(10850, "IFCLIGHTFIXTURETYPE");
            IfcIdTypeStringLookup.Add(10900, "IFCLINE");
            IfcIdTypeStringLookup.Add(10950, "IFCLOCALPLACEMENT");
            IfcIdTypeStringLookup.Add(11000, "IFCLOCALTIME");
            IfcIdTypeStringLookup.Add(11050, "IFCLOOP");
            IfcIdTypeStringLookup.Add(11100, "IFCLSHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(11150, "IFCMAPPEDITEM");
            IfcIdTypeStringLookup.Add(11200, "IFCMATERIAL");
            IfcIdTypeStringLookup.Add(11250, "IFCMATERIALCLASSIFICATIONRELATIONSHIP");
            IfcIdTypeStringLookup.Add(11300, "IFCMATERIALDEFINITIONREPRESENTATION");
            IfcIdTypeStringLookup.Add(11350, "IFCMATERIALLAYER");
            IfcIdTypeStringLookup.Add(11400, "IFCMATERIALLAYERSET");
            IfcIdTypeStringLookup.Add(11450, "IFCMATERIALLAYERSETUSAGE");
            IfcIdTypeStringLookup.Add(11500, "IFCMATERIALLIST");
            IfcIdTypeStringLookup.Add(11550, "IFCMEASUREWITHUNIT");
            IfcIdTypeStringLookup.Add(11600, "IFCMECHANICALFASTENER");
            IfcIdTypeStringLookup.Add(11650, "IFCMECHANICALFASTENERTYPE");
            IfcIdTypeStringLookup.Add(11700, "IFCMEMBER");
            IfcIdTypeStringLookup.Add(11750, "IFCMEMBERTYPE");
            IfcIdTypeStringLookup.Add(11800, "IFCMETRIC");
            IfcIdTypeStringLookup.Add(11850, "IFCMONETARYUNIT");
            IfcIdTypeStringLookup.Add(11900, "IFCMOTORCONNECTIONTYPE");
            IfcIdTypeStringLookup.Add(11950, "IFCOBJECTIVE");
            IfcIdTypeStringLookup.Add(12000, "IFCOFFSETCURVE2D");
            IfcIdTypeStringLookup.Add(12050, "IFCOFFSETCURVE3D");
            IfcIdTypeStringLookup.Add(12100, "IFCONEDIRECTIONREPEATFACTOR");
            IfcIdTypeStringLookup.Add(12150, "IFCOPENINGELEMENT");
            IfcIdTypeStringLookup.Add(12200, "IFCOPENSHELL");
            IfcIdTypeStringLookup.Add(12250, "IFCORGANIZATION");
            IfcIdTypeStringLookup.Add(12300, "IFCORGANIZATIONRELATIONSHIP");
            IfcIdTypeStringLookup.Add(12350, "IFCORIENTEDEDGE");
            IfcIdTypeStringLookup.Add(12400, "IFCOUTLETTYPE");
            IfcIdTypeStringLookup.Add(12450, "IFCOWNERHISTORY");
            IfcIdTypeStringLookup.Add(12500, "IFCPERSON");
            IfcIdTypeStringLookup.Add(12550, "IFCPERSONANDORGANIZATION");
            IfcIdTypeStringLookup.Add(12600, "IFCPHYSICALCOMPLEXQUANTITY");
            IfcIdTypeStringLookup.Add(12650, "IFCPILE");
            IfcIdTypeStringLookup.Add(12700, "IFCPIPEFITTINGTYPE");
            IfcIdTypeStringLookup.Add(12750, "IFCPIPESEGMENTTYPE");
            IfcIdTypeStringLookup.Add(12800, "IFCPLANAREXTENT");
            IfcIdTypeStringLookup.Add(12850, "IFCPLANE");
            IfcIdTypeStringLookup.Add(12900, "IFCPLATE");
            IfcIdTypeStringLookup.Add(12950, "IFCPLATETYPE");
            IfcIdTypeStringLookup.Add(13000, "IFCPOINTONCURVE");
            IfcIdTypeStringLookup.Add(13050, "IFCPOINTONSURFACE");
            IfcIdTypeStringLookup.Add(13100, "IFCPOLYGONALBOUNDEDHALFSPACE");
            IfcIdTypeStringLookup.Add(13150, "IFCPOLYLINE");
            IfcIdTypeStringLookup.Add(13200, "IFCPOLYLOOP");
            IfcIdTypeStringLookup.Add(13250, "IFCPOSTALADDRESS");
            IfcIdTypeStringLookup.Add(13300, "IFCPREDEFINEDSYMBOL");
            IfcIdTypeStringLookup.Add(13350, "IFCPRESENTATIONLAYERASSIGNMENT");
            IfcIdTypeStringLookup.Add(13400, "IFCPRESENTATIONLAYERWITHSTYLE");
            IfcIdTypeStringLookup.Add(13450, "IFCPRESENTATIONSTYLEASSIGNMENT");
            IfcIdTypeStringLookup.Add(13500, "IFCPRODUCTDEFINITIONSHAPE");
            IfcIdTypeStringLookup.Add(13550, "IFCPRODUCTREPRESENTATION");
            IfcIdTypeStringLookup.Add(13600, "IFCPROJECT");
            IfcIdTypeStringLookup.Add(13650, "IFCPROJECTIONELEMENT");
            IfcIdTypeStringLookup.Add(13700, "IFCPROPERTYBOUNDEDVALUE");
            IfcIdTypeStringLookup.Add(13750, "IFCPROPERTYCONSTRAINTRELATIONSHIP");
            IfcIdTypeStringLookup.Add(13800, "IFCPROPERTYDEPENDENCYRELATIONSHIP");
            IfcIdTypeStringLookup.Add(13850, "IFCPROPERTYENUMERATEDVALUE");
            IfcIdTypeStringLookup.Add(13900, "IFCPROPERTYENUMERATION");
            IfcIdTypeStringLookup.Add(13950, "IFCPROPERTYLISTVALUE");
            IfcIdTypeStringLookup.Add(14000, "IFCPROPERTYREFERENCEVALUE");
            IfcIdTypeStringLookup.Add(14050, "IFCPROPERTYSET");
            IfcIdTypeStringLookup.Add(14100, "IFCPROPERTYSINGLEVALUE");
            IfcIdTypeStringLookup.Add(14150, "IFCPROPERTYTABLEVALUE");
            IfcIdTypeStringLookup.Add(14200, "IFCPROTECTIVEDEVICETYPE");
            IfcIdTypeStringLookup.Add(14250, "IFCPROXY");
            IfcIdTypeStringLookup.Add(14300, "IFCPUMPTYPE");
            IfcIdTypeStringLookup.Add(14350, "IFCQUANTITYAREA");
            IfcIdTypeStringLookup.Add(14400, "IFCQUANTITYCOUNT");
            IfcIdTypeStringLookup.Add(14450, "IFCQUANTITYLENGTH");
            IfcIdTypeStringLookup.Add(14500, "IFCQUANTITYTIME");
            IfcIdTypeStringLookup.Add(14550, "IFCQUANTITYVOLUME");
            IfcIdTypeStringLookup.Add(14600, "IFCQUANTITYWEIGHT");
            IfcIdTypeStringLookup.Add(14650, "IFCRAILING");
            IfcIdTypeStringLookup.Add(14700, "IFCRAILINGTYPE");
            IfcIdTypeStringLookup.Add(14750, "IFCRAMP");
            IfcIdTypeStringLookup.Add(14800, "IFCRAMPFLIGHT");
            IfcIdTypeStringLookup.Add(14850, "IFCRAMPFLIGHTTYPE");
            IfcIdTypeStringLookup.Add(14900, "IFCRATIONALBEZIERCURVE");
            IfcIdTypeStringLookup.Add(14950, "IFCRECTANGLEHOLLOWPROFILEDEF");
            IfcIdTypeStringLookup.Add(15000, "IFCRECTANGLEPROFILEDEF");
            IfcIdTypeStringLookup.Add(15050, "IFCRECTANGULARTRIMMEDSURFACE");
            IfcIdTypeStringLookup.Add(15100, "IFCREFERENCESVALUEDOCUMENT");
            IfcIdTypeStringLookup.Add(15150, "IFCREGULARTIMESERIES");
            IfcIdTypeStringLookup.Add(15200, "IFCREINFORCEMENTBARPROPERTIES");
            IfcIdTypeStringLookup.Add(15250, "IFCREINFORCEMENTDEFINITIONPROPERTIES");
            IfcIdTypeStringLookup.Add(15300, "IFCREINFORCINGBAR");
            IfcIdTypeStringLookup.Add(15350, "IFCREINFORCINGMESH");
            IfcIdTypeStringLookup.Add(15400, "IFCRELAGGREGATES");
            IfcIdTypeStringLookup.Add(15450, "IFCRELASSIGNSTOACTOR");
            IfcIdTypeStringLookup.Add(15500, "IFCRELASSIGNSTOCONTROL");
            IfcIdTypeStringLookup.Add(15550, "IFCRELASSIGNSTOGROUP");
            IfcIdTypeStringLookup.Add(15600, "IFCRELASSIGNSTOPROCESS");
            IfcIdTypeStringLookup.Add(15650, "IFCRELASSIGNSTOPRODUCT");
            IfcIdTypeStringLookup.Add(15700, "IFCRELASSIGNSTORESOURCE");
            IfcIdTypeStringLookup.Add(15750, "IFCRELASSOCIATESAPPROVAL");
            IfcIdTypeStringLookup.Add(15800, "IFCRELASSOCIATESCLASSIFICATION");
            IfcIdTypeStringLookup.Add(15850, "IFCRELASSOCIATESDOCUMENT");
            IfcIdTypeStringLookup.Add(15900, "IFCRELASSOCIATESLIBRARY");
            IfcIdTypeStringLookup.Add(15950, "IFCRELASSOCIATESMATERIAL");
            IfcIdTypeStringLookup.Add(16000, "IFCRELASSOCIATESPROFILEPROPERTIES");
            IfcIdTypeStringLookup.Add(16050, "IFCRELCONNECTSELEMENTS");
            IfcIdTypeStringLookup.Add(16100, "IFCRELCONNECTSPATHELEMENTS");
            IfcIdTypeStringLookup.Add(16150, "IFCRELCONNECTSPORTS");
            IfcIdTypeStringLookup.Add(16200, "IFCRELCONNECTSPORTTOELEMENT");
            IfcIdTypeStringLookup.Add(16250, "IFCRELCONNECTSSTRUCTURALACTIVITY");
            IfcIdTypeStringLookup.Add(16300, "IFCRELCONNECTSSTRUCTURALELEMENT");
            IfcIdTypeStringLookup.Add(16350, "IFCRELCONNECTSSTRUCTURALMEMBER");
            IfcIdTypeStringLookup.Add(16400, "IFCRELCONNECTSWITHECCENTRICITY");
            IfcIdTypeStringLookup.Add(16450, "IFCRELCONNECTSWITHREALIZINGELEMENTS");
            IfcIdTypeStringLookup.Add(16500, "IFCRELCONTAINEDINSPATIALSTRUCTURE");
            IfcIdTypeStringLookup.Add(16550, "IFCRELCOVERSBLDGELEMENTS");
            IfcIdTypeStringLookup.Add(16600, "IFCRELCOVERSSPACES");
            IfcIdTypeStringLookup.Add(16650, "IFCRELDEFINESBYPROPERTIES");
            IfcIdTypeStringLookup.Add(16700, "IFCRELDEFINESBYTYPE");
            IfcIdTypeStringLookup.Add(16750, "IFCRELFILLSELEMENT");
            IfcIdTypeStringLookup.Add(16800, "IFCRELFLOWCONTROLELEMENTS");
            IfcIdTypeStringLookup.Add(16850, "IFCRELNESTS");
            IfcIdTypeStringLookup.Add(16900, "IFCRELOVERRIDESPROPERTIES");
            IfcIdTypeStringLookup.Add(16950, "IFCRELPROJECTSELEMENT");
            IfcIdTypeStringLookup.Add(17000, "IFCRELREFERENCEDINSPATIALSTRUCTURE");
            IfcIdTypeStringLookup.Add(17050, "IFCRELSEQUENCE");
            IfcIdTypeStringLookup.Add(17100, "IFCRELSERVICESBUILDINGS");
            IfcIdTypeStringLookup.Add(17150, "IFCRELSPACEBOUNDARY");
            IfcIdTypeStringLookup.Add(17200, "IFCRELVOIDSELEMENT");
            IfcIdTypeStringLookup.Add(17250, "IFCREPRESENTATION");
            IfcIdTypeStringLookup.Add(17300, "IFCREPRESENTATIONCONTEXT");
            IfcIdTypeStringLookup.Add(17350, "IFCREPRESENTATIONMAP");
            IfcIdTypeStringLookup.Add(17400, "IFCRESOURCEAPPROVALRELATIONSHIP");
            IfcIdTypeStringLookup.Add(17450, "IFCREVOLVEDAREASOLID");
            IfcIdTypeStringLookup.Add(17500, "IFCROOF");
            IfcIdTypeStringLookup.Add(17550, "IFCROUNDEDRECTANGLEPROFILEDEF");
            IfcIdTypeStringLookup.Add(17600, "IFCSANITARYTERMINALTYPE");
            IfcIdTypeStringLookup.Add(17650, "IFCSECTIONEDSPINE");
            IfcIdTypeStringLookup.Add(17700, "IFCSECTIONPROPERTIES");
            IfcIdTypeStringLookup.Add(17750, "IFCSECTIONREINFORCEMENTPROPERTIES");
            IfcIdTypeStringLookup.Add(17800, "IFCSENSORTYPE");
            IfcIdTypeStringLookup.Add(17850, "IFCSHAPEASPECT");
            IfcIdTypeStringLookup.Add(17900, "IFCSHAPEREPRESENTATION");
            IfcIdTypeStringLookup.Add(17950, "IFCSHELLBASEDSURFACEMODEL");
            IfcIdTypeStringLookup.Add(18000, "IFCSITE");
            IfcIdTypeStringLookup.Add(18050, "IFCSIUNIT");
            IfcIdTypeStringLookup.Add(18100, "IFCSLAB");
            IfcIdTypeStringLookup.Add(18150, "IFCSLABTYPE");
            IfcIdTypeStringLookup.Add(18200, "IFCSLIPPAGECONNECTIONCONDITION");
            IfcIdTypeStringLookup.Add(18250, "IFCSOUNDPROPERTIES");
            IfcIdTypeStringLookup.Add(18300, "IFCSOUNDVALUE");
            IfcIdTypeStringLookup.Add(18350, "IFCSPACE");
            IfcIdTypeStringLookup.Add(18400, "IFCSPACEHEATERTYPE");
            IfcIdTypeStringLookup.Add(18450, "IFCSPACETHERMALLOADPROPERTIES");
            IfcIdTypeStringLookup.Add(18500, "IFCSPACETYPE");
            IfcIdTypeStringLookup.Add(18550, "IFCSTACKTERMINALTYPE");
            IfcIdTypeStringLookup.Add(18600, "IFCSTAIR");
            IfcIdTypeStringLookup.Add(18650, "IFCSTAIRFLIGHT");
            IfcIdTypeStringLookup.Add(18700, "IFCSTAIRFLIGHTTYPE");
            IfcIdTypeStringLookup.Add(18750, "IFCSTRUCTURALANALYSISMODEL");
            IfcIdTypeStringLookup.Add(18800, "IFCSTRUCTURALCURVECONNECTION");
            IfcIdTypeStringLookup.Add(18850, "IFCSTRUCTURALCURVEMEMBER");
            IfcIdTypeStringLookup.Add(18900, "IFCSTRUCTURALCURVEMEMBERVARYING");
            IfcIdTypeStringLookup.Add(18950, "IFCSTRUCTURALLINEARACTION");
            IfcIdTypeStringLookup.Add(19000, "IFCSTRUCTURALLINEARACTIONVARYING");
            IfcIdTypeStringLookup.Add(19050, "IFCSTRUCTURALLOADGROUP");
            IfcIdTypeStringLookup.Add(19100, "IFCSTRUCTURALLOADLINEARFORCE");
            IfcIdTypeStringLookup.Add(19150, "IFCSTRUCTURALLOADPLANARFORCE");
            IfcIdTypeStringLookup.Add(19200, "IFCSTRUCTURALLOADSINGLEDISPLACEMENT");
            IfcIdTypeStringLookup.Add(19250, "IFCSTRUCTURALLOADSINGLEDISPLACEMENTDISTORTION");
            IfcIdTypeStringLookup.Add(19300, "IFCSTRUCTURALLOADSINGLEFORCE");
            IfcIdTypeStringLookup.Add(19350, "IFCSTRUCTURALLOADSINGLEFORCEWARPING");
            IfcIdTypeStringLookup.Add(19400, "IFCSTRUCTURALLOADTEMPERATURE");
            IfcIdTypeStringLookup.Add(19450, "IFCSTRUCTURALPLANARACTION");
            IfcIdTypeStringLookup.Add(19500, "IFCSTRUCTURALPLANARACTIONVARYING");
            IfcIdTypeStringLookup.Add(19550, "IFCSTRUCTURALPOINTACTION");
            IfcIdTypeStringLookup.Add(19600, "IFCSTRUCTURALPOINTCONNECTION");
            IfcIdTypeStringLookup.Add(19650, "IFCSTRUCTURALPOINTREACTION");
            IfcIdTypeStringLookup.Add(19700, "IFCSTRUCTURALPROFILEPROPERTIES");
            IfcIdTypeStringLookup.Add(19750, "IFCSTRUCTURALRESULTGROUP");
            IfcIdTypeStringLookup.Add(19800, "IFCSTRUCTURALSURFACECONNECTION");
            IfcIdTypeStringLookup.Add(19850, "IFCSTRUCTURALSURFACEMEMBER");
            IfcIdTypeStringLookup.Add(19900, "IFCSTRUCTURALSURFACEMEMBERVARYING");
            IfcIdTypeStringLookup.Add(19950, "IFCSTYLEDITEM");
            IfcIdTypeStringLookup.Add(20000, "IFCSTYLEDREPRESENTATION");
            IfcIdTypeStringLookup.Add(20050, "IFCSUBCONTRACTRESOURCE");
            IfcIdTypeStringLookup.Add(20100, "IFCSUBEDGE");
            IfcIdTypeStringLookup.Add(20150, "IFCSURFACECURVESWEPTAREASOLID");
            IfcIdTypeStringLookup.Add(20200, "IFCSURFACEOFLINEAREXTRUSION");
            IfcIdTypeStringLookup.Add(20250, "IFCSURFACEOFREVOLUTION");
            IfcIdTypeStringLookup.Add(20300, "IFCSURFACESTYLE");
            IfcIdTypeStringLookup.Add(20350, "IFCSURFACESTYLELIGHTING");
            IfcIdTypeStringLookup.Add(20400, "IFCSURFACESTYLEREFRACTION");
            IfcIdTypeStringLookup.Add(20450, "IFCSURFACESTYLERENDERING");
            IfcIdTypeStringLookup.Add(20500, "IFCSURFACESTYLESHADING");
            IfcIdTypeStringLookup.Add(20550, "IFCSURFACESTYLEWITHTEXTURES");
            IfcIdTypeStringLookup.Add(20600, "IFCSWEPTDISKSOLID");
            IfcIdTypeStringLookup.Add(20650, "IFCSWITCHINGDEVICETYPE");
            IfcIdTypeStringLookup.Add(20700, "IFCSYSTEM");
            IfcIdTypeStringLookup.Add(20750, "IFCSYSTEMFURNITUREELEMENTTYPE");
            IfcIdTypeStringLookup.Add(20800, "IFCTABLE");
            IfcIdTypeStringLookup.Add(20850, "IFCTABLEROW");
            IfcIdTypeStringLookup.Add(20900, "IFCTANKTYPE");
            IfcIdTypeStringLookup.Add(20950, "IFCTASK");
            IfcIdTypeStringLookup.Add(21000, "IFCTELECOMADDRESS");
            IfcIdTypeStringLookup.Add(21050, "IFCTENDON");
            IfcIdTypeStringLookup.Add(21100, "IFCTENDONANCHOR");
            IfcIdTypeStringLookup.Add(21150, "IFCTEXTLITERAL");
            IfcIdTypeStringLookup.Add(21200, "IFCTEXTLITERALWITHEXTENT");
            IfcIdTypeStringLookup.Add(21250, "IFCTEXTSTYLE");
            IfcIdTypeStringLookup.Add(21300, "IFCTEXTSTYLEFONTMODEL");
            IfcIdTypeStringLookup.Add(21350, "IFCTEXTSTYLEFORDEFINEDFONT");
            IfcIdTypeStringLookup.Add(21400, "IFCTEXTSTYLETEXTMODEL");
            IfcIdTypeStringLookup.Add(21450, "IFCTIMESERIESREFERENCERELATIONSHIP");
            IfcIdTypeStringLookup.Add(21500, "IFCTIMESERIESVALUE");
            IfcIdTypeStringLookup.Add(21550, "IFCTOPOLOGYREPRESENTATION");
            IfcIdTypeStringLookup.Add(21600, "IFCTRANSFORMERTYPE");
            IfcIdTypeStringLookup.Add(21650, "IFCTRANSPORTELEMENT");
            IfcIdTypeStringLookup.Add(21700, "IFCTRAPEZIUMPROFILEDEF");
            IfcIdTypeStringLookup.Add(21750, "IFCTRIMMEDCURVE");
            IfcIdTypeStringLookup.Add(21800, "IFCTSHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(21850, "IFCTUBEBUNDLETYPE");
            IfcIdTypeStringLookup.Add(21900, "IFCTWODIRECTIONREPEATFACTOR");
            IfcIdTypeStringLookup.Add(21950, "IFCTYPEOBJECT");
            IfcIdTypeStringLookup.Add(22000, "IFCTYPEPRODUCT");
            IfcIdTypeStringLookup.Add(22050, "IFCUNITARYEQUIPMENTTYPE");
            IfcIdTypeStringLookup.Add(22100, "IFCUNITASSIGNMENT");
            IfcIdTypeStringLookup.Add(22150, "IFCUSHAPEPROFILEDEF");
            IfcIdTypeStringLookup.Add(22200, "IFCVALVETYPE");
            IfcIdTypeStringLookup.Add(22250, "IFCVECTOR");
            IfcIdTypeStringLookup.Add(22300, "IFCVERTEX");
            IfcIdTypeStringLookup.Add(22350, "IFCVERTEXLOOP");
            IfcIdTypeStringLookup.Add(22400, "IFCVERTEXPOINT");
            IfcIdTypeStringLookup.Add(22450, "IFCVIBRATIONISOLATORTYPE");
            IfcIdTypeStringLookup.Add(22500, "IFCVIRTUALELEMENT");
            IfcIdTypeStringLookup.Add(22550, "IFCVIRTUALGRIDINTERSECTION");
            IfcIdTypeStringLookup.Add(22600, "IFCWALL");
            IfcIdTypeStringLookup.Add(22650, "IFCWALLSTANDARDCASE");
            IfcIdTypeStringLookup.Add(22700, "IFCWALLTYPE");
            IfcIdTypeStringLookup.Add(22750, "IFCWASTETERMINALTYPE");
            IfcIdTypeStringLookup.Add(22800, "IFCWINDOW");
            IfcIdTypeStringLookup.Add(22850, "IFCWINDOWLININGPROPERTIES");
            IfcIdTypeStringLookup.Add(22900, "IFCWINDOWPANELPROPERTIES");
            IfcIdTypeStringLookup.Add(22950, "IFCWINDOWSTYLE");
            IfcIdTypeStringLookup.Add(23000, "IFCZONE");
            IfcIdTypeStringLookup.Add(23050, "IFCZSHAPEPROFILEDEF");
            
        }

        private static Dictionary<string, IfcType> _IfcTypeLookup;

        public static Dictionary<string, IfcType> IfcTypeLookup
        {
            get { return _IfcTypeLookup; }
            
        }

        public static Dictionary<ushort, string> IfcIdTypeStringLookup
        {
            get { return _IfcIdTypeStringLookup; }
            
        }
        internal static void AddProperties(IfcType ifcType)
        {
            PropertyInfo[] properties =
                ifcType.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo propInfo in properties)
            {
                int attributeIdx = -1;
                IfcAttribute[] ifcAttributes =
                    (IfcAttribute[]) propInfo.GetCustomAttributes(typeof (IfcAttribute), false);
                if (ifcAttributes.GetLength(0) > 0) //we have an ifc property
                {
                    if (ifcAttributes[0].Order > 0)
                    {
                        ifcType.IfcProperties.Add(ifcAttributes[0].Order,
                                                  new IfcMetaProperty { PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0] });
                        attributeIdx = ifcAttributes[0].Order;
                    }

                    else
                        ifcType.IfcInverses.Add(new IfcMetaProperty { PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0] });
                }
                IfcIndex[] ifcPrimaryIndices =
                    (IfcIndex[]) propInfo.GetCustomAttributes(typeof (IfcIndex), false);
                if (ifcPrimaryIndices.GetLength(0) > 0) //we have an ifc primary index
                {
                    ifcType.PrimaryIndex = propInfo;
                    ifcType.PrimaryKeyIndex = attributeIdx;
                }
                IfcSecondaryIndex[] ifcSecondaryIndices =
                    (IfcSecondaryIndex[]) propInfo.GetCustomAttributes(typeof (IfcSecondaryIndex), false);
                if (ifcSecondaryIndices.GetLength(0) > 0) //we have an ifc primary index
                {
                    if (ifcType.SecondaryIndices == null) ifcType.SecondaryIndices = new List<PropertyInfo>();
                    ifcType.SecondaryIndices.Add(propInfo);
                }
            }
        }

        public static void GenerateSchema(TextWriter res)
        {
            IndentedTextWriter iw = new IndentedTextWriter(res);
            foreach (IfcType ifcType in IfcEntities)
            {
                iw.WriteLine(string.Format("ENTITY Ifc{0}", ifcType.Type.Name));
                if (ifcType.IfcSuperType != null)
                {
                    iw.Indent++;
                    iw.WriteLine(string.Format("SUBTYPE OF ({0})", ifcType.IfcSuperType.Type.Name));
                }
                if (ifcType.IfcProperties.Count > 0)
                {
                    iw.Indent++;
                    foreach (IfcMetaProperty prop in ifcType.IfcProperties.Values)
                    {
                        iw.WriteLine(string.Format("{0}\t: {1};", prop.PropertyInfo.Name,
                                                   prop.PropertyInfo.PropertyType.Name));
                    }
                    iw.Indent--;
                }
                if (ifcType.IfcInverses.Count > 0)
                {
                    iw.WriteLine("INVERSE");
                    iw.Indent++;
                    foreach (IfcMetaProperty prop in ifcType.IfcInverses)
                    {
                        iw.Write(string.Format("{0}\t: ", prop.PropertyInfo.Name));
                        int min = prop.IfcAttribute.MinCardinality;
                        int max = prop.IfcAttribute.MaxCardinality;
                        switch (prop.IfcAttribute.IfcType)
                        {
                            case IfcAttributeType.Set:
                                iw.WriteLine(string.Format("SET OF {0}:{1}", min > -1 ? "[" + min : "",
                                                           min > -1 ? max > 0 ? max + "] " : "?" + "] " : ""));
                                break;
                            case IfcAttributeType.List:
                            case IfcAttributeType.ListUnique:
                                iw.WriteLine(string.Format("LIST OF {0}:{1}", min > -1 ? "[" + min : "",
                                                           min > -1 ? max > 0 ? max + "] " : "?" + "] " : ""));
                                break;
                            default:
                                break;
                        }
                    }
                    iw.Indent--;
                }
                iw.Indent--;
                iw.WriteLine("END_ENTITY");
            }
        }

        internal static void AddParent(IfcType child)
        {
            Type baseParent = child.Type.BaseType;
            if (typeof (object) == baseParent || typeof (ValueType) == baseParent)
                return;
            IfcType ifcParent;
            if (!IfcEntities.Contains(baseParent))
            {
                IfcEntities.Add(ifcParent = new IfcType {Type = baseParent});
                string typeLookup = baseParent.Name.ToUpper();
                if (!IfcTypeLookup.ContainsKey(typeLookup))
                    IfcTypeLookup.Add(typeLookup, ifcParent);
                ifcParent.IfcSubTypes.Add(child);
                child.IfcSuperType = ifcParent;
                AddParent(ifcParent);
                AddProperties(ifcParent);
            }
            else
            {
                ifcParent = IfcEntities[baseParent];
                child.IfcSuperType = ifcParent;
                if (!ifcParent.IfcSubTypes.Contains(child))
                    ifcParent.IfcSubTypes.Add(child);
            }
        }

        public IfcInstances(IModel model)
        {
            _model = model;
        }

        public IfcInstances(IModel model, bool buildIndices)
        {
            _buildIndices = buildIndices;
            _model = model;
        }

        public override string ToString()
        {
            return string.Format("Count = {0}", Count);
        }

        public IEnumerable<TIfcType> OfType<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            Type type = typeof (TIfcType);
            IfcType ifcType = IfcEntities[type];
            foreach (Type item in ifcType.NonAbstractSubTypes)
            {
                ICollection<long> entities;
                if (_typeLookup.TryGetValue(item, out entities))
                {
                    foreach (long label in entities)
                    {
                        yield return (TIfcType)_entityHandleLookup.GetOrCreateEntity(_model, label);
                    }
                }
            }
        }


        public void CopyTo(IfcInstances copyTo, Type type)
        {
            foreach (KeyValuePair<Type, ICollection<long>> label in _typeLookup)
            {
                ICollection<long> entities;
                if (_typeLookup.TryGetValue(type, out entities))
                {
                    foreach (long entLabel in entities)
                    {
                        copyTo.Add_Reversible(_entityHandleLookup.GetEntity(entLabel));
                    }
                }
            }
        }

        #region IEnumerable<object> Members

       

        #endregion

        #region IEnumerable Members

       

        #endregion

        #region ICollection<object> Dummy Members

        private void Add_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add_Reversible(new KeyValuePair<Type, ICollection<long>>(type, entities));
            }
            entities.Add_Reversible(instance.EntityLabel);
            _entityHandleLookup.Add_Reversible(instance);
        }

        public IPersistIfcEntity GetInstance(long label)
        {
            return _entityHandleLookup.GetEntity(label);
        }

        public bool ContainsInstance(long label)
        {
            return _entityHandleLookup.Keys.Contains(label);
        }

        internal void AddRaw(IPersistIfcEntity instance)
        {
            AddTypeReference(instance);
            _entityHandleLookup.Add(instance);
        }

        /// <summary>
        /// Adds the instance to the tpye referncing dictionaries, this is NOT an undoable action
        /// </summary>
        /// <param name="instance"></param>
        private void AddTypeReference(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();

                _typeLookup.Add(type, entities);
            }
            entities.Add(instance.EntityLabel);
        }
         /// <summary>
        /// creates and adds a new entity to the model, this operation is NOT undoable
        /// </summary>
        /// <param name="xbimModel"></param>
        /// <param name="ifcType"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfcEntity AddNew(XbimModel xbimModel, Type ifcType, long label)
        {

            IPersistIfcEntity ent = _entityHandleLookup.CreateEntity(xbimModel, ifcType, label);
            _highestLabel = Math.Max(label, _highestLabel);
            AddTypeReference(ent);
            return ent;
        }


        /// <summary>
        /// creates and adds a new entity to the model, this operation is undoable
        /// </summary>
        /// <param name="xbimModel"></param>
        /// <param name="ifcType"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        internal IPersistIfcEntity AddNew_Reversable(IModel xbimModel, Type ifcType, long label)
        {
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, Math.Max(label, _highestLabel), label);
            IPersistIfcEntity ent = _entityHandleLookup.CreateEntity(xbimModel, ifcType, label);
            _highestLabel = Math.Max(label, _highestLabel);
            AddTypeReference_Reversable(ent);
            return ent;
        }
        /// <summary>
        /// creates and adds a new entity to the model, this operation is undoable
        /// </summary>
        /// <param name="xbimModel"></param>
        /// <param name="ifcType"></param>
        /// <returns></returns>
        internal IPersistIfcEntity AddNew_Reversable(IModel xbimModel, Type ifcType)
        {
            long label = NextLabel();
            return AddNew_Reversable(xbimModel, ifcType, label);
        }

        public void Add(IPersistIfcEntity instance)
        {
            AddTypeReference_Reversable(instance);
            _entityHandleLookup.Add(instance);
            
        }

        private void AddTypeReference_Reversable(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add_Reversible(type, entities);
            }
            entities.Add_Reversible(instance.EntityLabel);
        }

        public void Clear_Reversible()
        {
            _typeLookup.Clear_Reversible();
            _entityHandleLookup.Clear_Reversible();
        }

        public bool Contains(IPersistIfcEntity instance)
        {
            return _entityHandleLookup.Contains(instance.EntityLabel);
        }

        /// <summary>
        ///   Copies all instances of the specified type into copyTo. This is a reversable action
        /// </summary>
        /// <param name = "copyTo"></param>
        public void CopyTo<TIfcType>(IfcInstances copyTo) where TIfcType : IPersistIfcEntity
        {
            foreach (TIfcType item in this.OfType<TIfcType>())
                copyTo.Add_Reversible(item);
        }

        public int Count
        {
            get
            {
                return _entityHandleLookup.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary) _typeLookup).IsReadOnly; }
        }

        private bool Remove_Reversible(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                entities.Remove_Reversible(instance.EntityLabel);
            return _entityHandleLookup.Remove_Reversible(instance.EntityLabel);
        }

        public bool Remove(IPersistIfcEntity instance)
        {
            Type type = instance.GetType();
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                 entities.Remove(instance.EntityLabel);
            return _entityHandleLookup.Remove(instance.EntityLabel);
        }

        #endregion

        public bool TryGetValue(Type key, out ICollection<long> value)
        {
            return _typeLookup.TryGetValue(key, out value);
        }

        #region ICollection<IPersistIfc> Members

        public void Clear()
        {
            _typeLookup.Clear();
            _entityHandleLookup.Clear();
        }

        #endregion

        #region ICollection<IPersistIfc> Members

        public void CopyTo(IPersistIfcEntity[] array, int arrayIndex)
        {
            int i = arrayIndex;
            foreach (IPersistIfcEntity item in this)
            {
                array[i] = item;
                i++;
            }
        }

        #endregion

        public ICollection<Type> Types
        {
            get { return _typeLookup.Keys; }
        }

        public ICollection<long> this[Type type]
        {
            get { return _typeLookup[type]; }
        }


        public int BuildIndices()
        {
            int err = 0;
            foreach (IfcType ifcType in IfcEntities)
            {
                
                if (!ifcType.Type.IsAbstract && ifcType.HasIndex)
                {
                    ICollection<long> entities;
                    if (_typeLookup.TryGetValue(ifcType.Type, out entities))
                    {
                        try
                        {
                            XbimIndexedCollection<long> index =
                                new XbimIndexedCollection<long>(ifcType.PrimaryIndex,
                                                                             ifcType.SecondaryIndices, entities);
                            _typeLookup.Remove(ifcType.Type);
                            _typeLookup.Add(ifcType.Type, index);
                        }
                        catch (Exception)
                        {
                            err++;
                            PropertyInfo pi = ifcType.PrimaryIndex;
                            Logger.WarnFormat("{0} is defined as a unique key in {1}, Duplicate values found. Index could not be built",
                                    ifcType.PrimaryIndex.Name, ifcType.Type.Name);
                        }
                    }
                }
            }
            return err;
        }


        public bool ContainsKey(Type key)
        {
            return _typeLookup.ContainsKey(key);
        }

        internal void Add(XbimInstanceHandle xbimInstanceHandle)
        {
            Type type = xbimInstanceHandle.EntityType;
            ICollection<long> entities;
            if (!_typeLookup.TryGetValue(type, out entities))
            {
                IfcType ifcType = IfcEntities[type];
                if (_buildIndices && ifcType.PrimaryIndex != null)
                    entities = new XbimIndexedCollection<long>(ifcType.SecondaryIndices);
                else
                    entities = new List<long>();
                _typeLookup.Add(type, entities);
            }
            entities.Add(xbimInstanceHandle.EntityLabel);
            _entityHandleLookup.Add(xbimInstanceHandle.EntityLabel, xbimInstanceHandle);

        }

        internal void DropAll()
        {
            IfcInstanceKeyedCollection newEntityHandleLookup = new IfcInstanceKeyedCollection();
            foreach (IXbimInstance item in _entityHandleLookup.Values)
            {
                newEntityHandleLookup.Add(item.EntityLabel, new XbimInstanceHandle(item));
            }
            _entityHandleLookup = newEntityHandleLookup;
        }

        internal IPersistIfcEntity GetOrCreateEntity(long label, out long fileOffset)
        {
            return _entityHandleLookup.GetOrCreateEntity(_model, label, out fileOffset);
        }

        internal IPersistIfcEntity GetOrCreateEntity(long label)
        {
            return _entityHandleLookup.GetOrCreateEntity(_model, label);
        }

        internal bool Contains(long entityLabel)
        {
            return _entityHandleLookup.Contains(entityLabel);
        }

        public IEnumerator<IPersistIfcEntity> GetEnumerator()
        {
            return _entityHandleLookup.GetEnumerator(_model);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _entityHandleLookup.GetEnumerator(_model);
        }

        internal long GetFileOffset(long label)
        {
            return _entityHandleLookup.GetFileOffset(label);
        }

        internal long InstancesOfTypeCount(Type type)
        {
            ICollection<long> entities;
            if (_typeLookup.TryGetValue(type, out entities))
                return entities.Count;
            else
                return 0;
        }

        IEnumerator<long> IEnumerable<long>.GetEnumerator()
        {
            return _entityHandleLookup.Keys.GetEnumerator();
        }

        //temp methhod should be removed
        internal Parser.XbimIndexEntry GetXbimIndexEntry(long p)
        {
            IXbimInstance inst = _entityHandleLookup[p];
            return new Xbim.IO.Parser.XbimIndexEntry(inst.EntityLabel,inst.FileOffset,inst.EntityType);
        }




       
    }
   

}