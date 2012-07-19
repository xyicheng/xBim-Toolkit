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
using Xbim.Ifc2x3.SelectTypes;
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
        private static Dictionary<short, string> _IfcIdTypeLookup = new Dictionary<short, string>();
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
                foreach (var item in _IfcIdTypeLookup)
                {
                    _IfcTypeLookup[item.Value].TypeId = item.Key;
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error reading Ifc Entity Meta Data", e);
            }
        }

        private static void InitIdTypeLookup()
        {
            IfcIdTypeLookup.Add(50, "IFC2DCOMPOSITECURVE");
            IfcIdTypeLookup.Add(100, "IFCACTOR");
            IfcIdTypeLookup.Add(150, "IFCACTORROLE");
            IfcIdTypeLookup.Add(200, "IFCACTUATORTYPE");
            IfcIdTypeLookup.Add(250, "IFCAIRTERMINALBOXTYPE");
            IfcIdTypeLookup.Add(300, "IFCAIRTERMINALTYPE");
            IfcIdTypeLookup.Add(350, "IFCAIRTOAIRHEATRECOVERYTYPE");
            IfcIdTypeLookup.Add(400, "IFCALARMTYPE");
            IfcIdTypeLookup.Add(450, "IFCANNOTATION");
            IfcIdTypeLookup.Add(500, "IFCANNOTATIONCURVEOCCURRENCE");
            IfcIdTypeLookup.Add(550, "IFCANNOTATIONFILLAREA");
            IfcIdTypeLookup.Add(600, "IFCANNOTATIONFILLAREAOCCURRENCE");
            IfcIdTypeLookup.Add(650, "IFCANNOTATIONSURFACEOCCURRENCE");
            IfcIdTypeLookup.Add(700, "IFCANNOTATIONSYMBOLOCCURRENCE");
            IfcIdTypeLookup.Add(750, "IFCANNOTATIONTEXTOCCURRENCE");
            IfcIdTypeLookup.Add(800, "IFCAPPLICATION");
            IfcIdTypeLookup.Add(850, "IFCAPPLIEDVALUERELATIONSHIP");
            IfcIdTypeLookup.Add(900, "IFCAPPROVAL");
            IfcIdTypeLookup.Add(950, "IFCAPPROVALACTORRELATIONSHIP");
            IfcIdTypeLookup.Add(1000, "IFCAPPROVALPROPERTYRELATIONSHIP");
            IfcIdTypeLookup.Add(1050, "IFCAPPROVALRELATIONSHIP");
            IfcIdTypeLookup.Add(1100, "IFCARBITRARYCLOSEDPROFILEDEF");
            IfcIdTypeLookup.Add(1150, "IFCARBITRARYOPENPROFILEDEF");
            IfcIdTypeLookup.Add(1200, "IFCARBITRARYPROFILEDEFWITHVOIDS");
            IfcIdTypeLookup.Add(1250, "IFCASYMMETRICISHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(1300, "IFCAXIS1PLACEMENT");
            IfcIdTypeLookup.Add(1350, "IFCAXIS2PLACEMENT2D");
            IfcIdTypeLookup.Add(1400, "IFCAXIS2PLACEMENT3D");
            IfcIdTypeLookup.Add(1450, "IFCBEAM");
            IfcIdTypeLookup.Add(1500, "IFCBEAMTYPE");
            IfcIdTypeLookup.Add(1550, "IFCBEZIERCURVE");
            IfcIdTypeLookup.Add(1600, "IFCBOILERTYPE");
            IfcIdTypeLookup.Add(1650, "IFCBOOLEANCLIPPINGRESULT");
            IfcIdTypeLookup.Add(1700, "IFCBOOLEANRESULT");
            IfcIdTypeLookup.Add(1750, "IFCBOUNDARYEDGECONDITION");
            IfcIdTypeLookup.Add(1800, "IFCBOUNDARYFACECONDITION");
            IfcIdTypeLookup.Add(1850, "IFCBOUNDARYNODECONDITION");
            IfcIdTypeLookup.Add(1900, "IFCBOUNDARYNODECONDITIONWARPING");
            IfcIdTypeLookup.Add(1950, "IFCBOUNDINGBOX");
            IfcIdTypeLookup.Add(2000, "IFCBOXEDHALFSPACE");
            IfcIdTypeLookup.Add(2050, "IFCBUILDING");
            IfcIdTypeLookup.Add(2100, "IFCBUILDINGELEMENTPART");
            IfcIdTypeLookup.Add(2150, "IFCBUILDINGELEMENTPROXY");
            IfcIdTypeLookup.Add(2200, "IFCBUILDINGELEMENTPROXYTYPE");
            IfcIdTypeLookup.Add(2250, "IFCBUILDINGSTOREY");
            IfcIdTypeLookup.Add(2300, "IFCCABLECARRIERFITTINGTYPE");
            IfcIdTypeLookup.Add(2350, "IFCCABLECARRIERSEGMENTTYPE");
            IfcIdTypeLookup.Add(2400, "IFCCABLESEGMENTTYPE");
            IfcIdTypeLookup.Add(2450, "IFCCALENDARDATE");
            IfcIdTypeLookup.Add(2500, "IFCCARTESIANPOINT");
            IfcIdTypeLookup.Add(2550, "IFCCARTESIANTRANSFORMATIONOPERATOR2D");
            IfcIdTypeLookup.Add(2600, "IFCCARTESIANTRANSFORMATIONOPERATOR2DNONUNIFORM");
            IfcIdTypeLookup.Add(2650, "IFCCARTESIANTRANSFORMATIONOPERATOR3D");
            IfcIdTypeLookup.Add(2700, "IFCCARTESIANTRANSFORMATIONOPERATOR3DNONUNIFORM");
            IfcIdTypeLookup.Add(2750, "IFCCENTERLINEPROFILEDEF");
            IfcIdTypeLookup.Add(2800, "IFCCHILLERTYPE");
            IfcIdTypeLookup.Add(2850, "IFCCIRCLE");
            IfcIdTypeLookup.Add(2900, "IFCCIRCLEHOLLOWPROFILEDEF");
            IfcIdTypeLookup.Add(2950, "IFCCIRCLEPROFILEDEF");
            IfcIdTypeLookup.Add(3000, "IFCCLASSIFICATION");
            IfcIdTypeLookup.Add(3050, "IFCCLASSIFICATIONITEM");
            IfcIdTypeLookup.Add(3100, "IFCCLASSIFICATIONITEMRELATIONSHIP");
            IfcIdTypeLookup.Add(3150, "IFCCLASSIFICATIONNOTATION");
            IfcIdTypeLookup.Add(3200, "IFCCLASSIFICATIONNOTATIONFACET");
            IfcIdTypeLookup.Add(3250, "IFCCLASSIFICATIONREFERENCE");
            IfcIdTypeLookup.Add(3300, "IFCCLOSEDSHELL");
            IfcIdTypeLookup.Add(3350, "IFCCOILTYPE");
            IfcIdTypeLookup.Add(3400, "IFCCOLOURRGB");
            IfcIdTypeLookup.Add(3450, "IFCCOLUMN");
            IfcIdTypeLookup.Add(3500, "IFCCOLUMNTYPE");
            IfcIdTypeLookup.Add(3550, "IFCCOMPLEXPROPERTY");
            IfcIdTypeLookup.Add(3600, "IFCCOMPOSITECURVE");
            IfcIdTypeLookup.Add(3650, "IFCCOMPOSITECURVESEGMENT");
            IfcIdTypeLookup.Add(3700, "IFCCOMPOSITEPROFILEDEF");
            IfcIdTypeLookup.Add(3750, "IFCCOMPRESSORTYPE");
            IfcIdTypeLookup.Add(3800, "IFCCONDENSERTYPE");
            IfcIdTypeLookup.Add(3850, "IFCCONNECTEDFACESET");
            IfcIdTypeLookup.Add(3900, "IFCCONNECTIONCURVEGEOMETRY");
            IfcIdTypeLookup.Add(3950, "IFCCONNECTIONPOINTGEOMETRY");
            IfcIdTypeLookup.Add(4000, "IFCCONNECTIONSURFACEGEOMETRY");
            IfcIdTypeLookup.Add(4050, "IFCCONSTRAINTAGGREGATIONRELATIONSHIP");
            IfcIdTypeLookup.Add(4100, "IFCCONSTRAINTCLASSIFICATIONRELATIONSHIP");
            IfcIdTypeLookup.Add(4150, "IFCCONSTRAINTRELATIONSHIP");
            IfcIdTypeLookup.Add(4200, "IFCCONSTRUCTIONEQUIPMENTRESOURCE");
            IfcIdTypeLookup.Add(4250, "IFCCONSTRUCTIONMATERIALRESOURCE");
            IfcIdTypeLookup.Add(4300, "IFCCONSTRUCTIONPRODUCTRESOURCE");
            IfcIdTypeLookup.Add(4350, "IFCCONTEXTDEPENDENTUNIT");
            IfcIdTypeLookup.Add(4400, "IFCCONTROLLERTYPE");
            IfcIdTypeLookup.Add(4450, "IFCCONVERSIONBASEDUNIT");
            IfcIdTypeLookup.Add(4500, "IFCCOOLEDBEAMTYPE");
            IfcIdTypeLookup.Add(4550, "IFCCOOLINGTOWERTYPE");
            IfcIdTypeLookup.Add(4600, "IFCCOORDINATEDUNIVERSALTIMEOFFSET");
            IfcIdTypeLookup.Add(4650, "IFCCOSTVALUE");
            IfcIdTypeLookup.Add(4700, "IFCCOVERING");
            IfcIdTypeLookup.Add(4750, "IFCCOVERINGTYPE");
            IfcIdTypeLookup.Add(4800, "IFCCRANERAILASHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(4850, "IFCCRANERAILFSHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(4900, "IFCCREWRESOURCE");
            IfcIdTypeLookup.Add(4950, "IFCCSGSOLID");
            IfcIdTypeLookup.Add(5000, "IFCCSHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(5050, "IFCCURRENCYRELATIONSHIP");
            IfcIdTypeLookup.Add(5100, "IFCCURTAINWALL");
            IfcIdTypeLookup.Add(5150, "IFCCURTAINWALLTYPE");
            IfcIdTypeLookup.Add(5200, "IFCCURVEBOUNDEDPLANE");
            IfcIdTypeLookup.Add(5250, "IFCCURVESTYLE");
            IfcIdTypeLookup.Add(5300, "IFCCURVESTYLEFONT");
            IfcIdTypeLookup.Add(5350, "IFCCURVESTYLEFONTANDSCALING");
            IfcIdTypeLookup.Add(5400, "IFCCURVESTYLEFONTPATTERN");
            IfcIdTypeLookup.Add(5450, "IFCDAMPERTYPE");
            IfcIdTypeLookup.Add(5500, "IFCDATEANDTIME");
            IfcIdTypeLookup.Add(5550, "IFCDEFINEDSYMBOL");
            IfcIdTypeLookup.Add(5600, "IFCDERIVEDPROFILEDEF");
            IfcIdTypeLookup.Add(5650, "IFCDERIVEDUNIT");
            IfcIdTypeLookup.Add(5700, "IFCDERIVEDUNITELEMENT");
            IfcIdTypeLookup.Add(5750, "IFCDIMENSIONALEXPONENTS");
            IfcIdTypeLookup.Add(5800, "IFCDIRECTION");
            IfcIdTypeLookup.Add(5850, "IFCDISCRETEACCESSORY");
            IfcIdTypeLookup.Add(5900, "IFCDISCRETEACCESSORYTYPE");
            IfcIdTypeLookup.Add(5950, "IFCDISTRIBUTIONCHAMBERELEMENT");
            IfcIdTypeLookup.Add(6000, "IFCDISTRIBUTIONCHAMBERELEMENTTYPE");
            IfcIdTypeLookup.Add(6050, "IFCDISTRIBUTIONCONTROLELEMENT");
            IfcIdTypeLookup.Add(6100, "IFCDISTRIBUTIONELEMENT");
            IfcIdTypeLookup.Add(6150, "IFCDISTRIBUTIONELEMENTTYPE");
            IfcIdTypeLookup.Add(6200, "IFCDISTRIBUTIONFLOWELEMENT");
            IfcIdTypeLookup.Add(6250, "IFCDISTRIBUTIONPORT");
            IfcIdTypeLookup.Add(6300, "IFCDOCUMENTELECTRONICFORMAT");
            IfcIdTypeLookup.Add(6350, "IFCDOCUMENTINFORMATION");
            IfcIdTypeLookup.Add(6400, "IFCDOCUMENTINFORMATIONRELATIONSHIP");
            IfcIdTypeLookup.Add(6450, "IFCDOCUMENTREFERENCE");
            IfcIdTypeLookup.Add(6500, "IFCDOOR");
            IfcIdTypeLookup.Add(6550, "IFCDOORLININGPROPERTIES");
            IfcIdTypeLookup.Add(6600, "IFCDOORPANELPROPERTIES");
            IfcIdTypeLookup.Add(6650, "IFCDOORSTYLE");
            IfcIdTypeLookup.Add(6700, "IFCDRAUGHTINGCALLOUT");
            IfcIdTypeLookup.Add(6750, "IFCDRAUGHTINGPREDEFINEDCOLOUR");
            IfcIdTypeLookup.Add(6800, "IFCDRAUGHTINGPREDEFINEDCURVEFONT");
            IfcIdTypeLookup.Add(6850, "IFCDUCTFITTINGTYPE");
            IfcIdTypeLookup.Add(6900, "IFCDUCTSEGMENTTYPE");
            IfcIdTypeLookup.Add(6950, "IFCDUCTSILENCERTYPE");
            IfcIdTypeLookup.Add(7000, "IFCEDGE");
            IfcIdTypeLookup.Add(7050, "IFCEDGECURVE");
            IfcIdTypeLookup.Add(7100, "IFCEDGELOOP");
            IfcIdTypeLookup.Add(7150, "IFCELECTRICALBASEPROPERTIES");
            IfcIdTypeLookup.Add(7200, "IFCELECTRICALCIRCUIT");
            IfcIdTypeLookup.Add(7250, "IFCELECTRICALELEMENT");
            IfcIdTypeLookup.Add(7300, "IFCELECTRICAPPLIANCETYPE");
            IfcIdTypeLookup.Add(7350, "IFCELECTRICDISTRIBUTIONPOINT");
            IfcIdTypeLookup.Add(7400, "IFCELECTRICFLOWSTORAGEDEVICETYPE");
            IfcIdTypeLookup.Add(7450, "IFCELECTRICGENERATORTYPE");
            IfcIdTypeLookup.Add(7500, "IFCELECTRICHEATERTYPE");
            IfcIdTypeLookup.Add(7550, "IFCELECTRICMOTORTYPE");
            IfcIdTypeLookup.Add(7600, "IFCELECTRICTIMECONTROLTYPE");
            IfcIdTypeLookup.Add(7650, "IFCELEMENTASSEMBLY");
            IfcIdTypeLookup.Add(7700, "IFCELEMENTQUANTITY");
            IfcIdTypeLookup.Add(7750, "IFCELLIPSE");
            IfcIdTypeLookup.Add(7800, "IFCELLIPSEPROFILEDEF");
            IfcIdTypeLookup.Add(7850, "IFCENERGYCONVERSIONDEVICE");
            IfcIdTypeLookup.Add(7900, "IFCENERGYPROPERTIES");
            IfcIdTypeLookup.Add(7950, "IFCENVIRONMENTALIMPACTVALUE");
            IfcIdTypeLookup.Add(8000, "IFCEQUIPMENTELEMENT");
            IfcIdTypeLookup.Add(8050, "IFCEVAPORATIVECOOLERTYPE");
            IfcIdTypeLookup.Add(8100, "IFCEVAPORATORTYPE");
            IfcIdTypeLookup.Add(8150, "IFCEXTENDEDMATERIALPROPERTIES");
            IfcIdTypeLookup.Add(8200, "IFCEXTERNALLYDEFINEDSURFACESTYLE");
            IfcIdTypeLookup.Add(8250, "IFCEXTERNALLYDEFINEDSYMBOL");
            IfcIdTypeLookup.Add(8300, "IFCEXTERNALLYDEFINEDTEXTFONT");
            IfcIdTypeLookup.Add(8350, "IFCEXTRUDEDAREASOLID");
            IfcIdTypeLookup.Add(8400, "IFCFACE");
            IfcIdTypeLookup.Add(8450, "IFCFACEBASEDSURFACEMODEL");
            IfcIdTypeLookup.Add(8500, "IFCFACEBOUND");
            IfcIdTypeLookup.Add(8550, "IFCFACEOUTERBOUND");
            IfcIdTypeLookup.Add(8600, "IFCFACESURFACE");
            IfcIdTypeLookup.Add(8650, "IFCFACETEDBREP");
            IfcIdTypeLookup.Add(8700, "IFCFACETEDBREPWITHVOIDS");
            IfcIdTypeLookup.Add(8750, "IFCFAILURECONNECTIONCONDITION");
            IfcIdTypeLookup.Add(8800, "IFCFANTYPE");
            IfcIdTypeLookup.Add(8850, "IFCFASTENER");
            IfcIdTypeLookup.Add(8900, "IFCFASTENERTYPE");
            IfcIdTypeLookup.Add(8950, "IFCFILLAREASTYLE");
            IfcIdTypeLookup.Add(9000, "IFCFILLAREASTYLEHATCHING");
            IfcIdTypeLookup.Add(9050, "IFCFILTERTYPE");
            IfcIdTypeLookup.Add(9100, "IFCFIRESUPPRESSIONTERMINALTYPE");
            IfcIdTypeLookup.Add(9150, "IFCFLOWCONTROLLER");
            IfcIdTypeLookup.Add(9200, "IFCFLOWFITTING");
            IfcIdTypeLookup.Add(9250, "IFCFLOWINSTRUMENTTYPE");
            IfcIdTypeLookup.Add(9300, "IFCFLOWMETERTYPE");
            IfcIdTypeLookup.Add(9350, "IFCFLOWMOVINGDEVICE");
            IfcIdTypeLookup.Add(9400, "IFCFLOWSEGMENT");
            IfcIdTypeLookup.Add(9450, "IFCFLOWSTORAGEDEVICE");
            IfcIdTypeLookup.Add(9500, "IFCFLOWTERMINAL");
            IfcIdTypeLookup.Add(9550, "IFCFLOWTREATMENTDEVICE");
            IfcIdTypeLookup.Add(9600, "IFCFLUIDFLOWPROPERTIES");
            IfcIdTypeLookup.Add(9650, "IFCFOOTING");
            IfcIdTypeLookup.Add(9700, "IFCFURNISHINGELEMENT");
            IfcIdTypeLookup.Add(9750, "IFCFURNISHINGELEMENTTYPE");
            IfcIdTypeLookup.Add(9800, "IFCFURNITURETYPE");
            IfcIdTypeLookup.Add(9850, "IFCGENERALPROFILEPROPERTIES");
            IfcIdTypeLookup.Add(9900, "IFCGEOMETRICCURVESET");
            IfcIdTypeLookup.Add(9950, "IFCGEOMETRICREPRESENTATIONCONTEXT");
            IfcIdTypeLookup.Add(10000, "IFCGEOMETRICREPRESENTATIONSUBCONTEXT");
            IfcIdTypeLookup.Add(10050, "IFCGEOMETRICSET");
            IfcIdTypeLookup.Add(10100, "IFCGRID");
            IfcIdTypeLookup.Add(10150, "IFCGRIDAXIS");
            IfcIdTypeLookup.Add(10200, "IFCGRIDPLACEMENT");
            IfcIdTypeLookup.Add(10250, "IFCGROUP");
            IfcIdTypeLookup.Add(10300, "IFCHALFSPACESOLID");
            IfcIdTypeLookup.Add(10350, "IFCHEATEXCHANGERTYPE");
            IfcIdTypeLookup.Add(10400, "IFCHUMIDIFIERTYPE");
            IfcIdTypeLookup.Add(10450, "IFCIRREGULARTIMESERIES");
            IfcIdTypeLookup.Add(10500, "IFCIRREGULARTIMESERIESVALUE");
            IfcIdTypeLookup.Add(10550, "IFCISHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(10600, "IFCJUNCTIONBOXTYPE");
            IfcIdTypeLookup.Add(10650, "IFCLABORRESOURCE");
            IfcIdTypeLookup.Add(10700, "IFCLAMPTYPE");
            IfcIdTypeLookup.Add(10750, "IFCLIBRARYINFORMATION");
            IfcIdTypeLookup.Add(10800, "IFCLIBRARYREFERENCE");
            IfcIdTypeLookup.Add(10850, "IFCLIGHTFIXTURETYPE");
            IfcIdTypeLookup.Add(10900, "IFCLINE");
            IfcIdTypeLookup.Add(10950, "IFCLOCALPLACEMENT");
            IfcIdTypeLookup.Add(11000, "IFCLOCALTIME");
            IfcIdTypeLookup.Add(11050, "IFCLOOP");
            IfcIdTypeLookup.Add(11100, "IFCLSHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(11150, "IFCMAPPEDITEM");
            IfcIdTypeLookup.Add(11200, "IFCMATERIAL");
            IfcIdTypeLookup.Add(11250, "IFCMATERIALCLASSIFICATIONRELATIONSHIP");
            IfcIdTypeLookup.Add(11300, "IFCMATERIALDEFINITIONREPRESENTATION");
            IfcIdTypeLookup.Add(11350, "IFCMATERIALLAYER");
            IfcIdTypeLookup.Add(11400, "IFCMATERIALLAYERSET");
            IfcIdTypeLookup.Add(11450, "IFCMATERIALLAYERSETUSAGE");
            IfcIdTypeLookup.Add(11500, "IFCMATERIALLIST");
            IfcIdTypeLookup.Add(11550, "IFCMEASUREWITHUNIT");
            IfcIdTypeLookup.Add(11600, "IFCMECHANICALFASTENER");
            IfcIdTypeLookup.Add(11650, "IFCMECHANICALFASTENERTYPE");
            IfcIdTypeLookup.Add(11700, "IFCMEMBER");
            IfcIdTypeLookup.Add(11750, "IFCMEMBERTYPE");
            IfcIdTypeLookup.Add(11800, "IFCMETRIC");
            IfcIdTypeLookup.Add(11850, "IFCMONETARYUNIT");
            IfcIdTypeLookup.Add(11900, "IFCMOTORCONNECTIONTYPE");
            IfcIdTypeLookup.Add(11950, "IFCOBJECTIVE");
            IfcIdTypeLookup.Add(12000, "IFCOFFSETCURVE2D");
            IfcIdTypeLookup.Add(12050, "IFCOFFSETCURVE3D");
            IfcIdTypeLookup.Add(12100, "IFCONEDIRECTIONREPEATFACTOR");
            IfcIdTypeLookup.Add(12150, "IFCOPENINGELEMENT");
            IfcIdTypeLookup.Add(12200, "IFCOPENSHELL");
            IfcIdTypeLookup.Add(12250, "IFCORGANIZATION");
            IfcIdTypeLookup.Add(12300, "IFCORGANIZATIONRELATIONSHIP");
            IfcIdTypeLookup.Add(12350, "IFCORIENTEDEDGE");
            IfcIdTypeLookup.Add(12400, "IFCOUTLETTYPE");
            IfcIdTypeLookup.Add(12450, "IFCOWNERHISTORY");
            IfcIdTypeLookup.Add(12500, "IFCPERSON");
            IfcIdTypeLookup.Add(12550, "IFCPERSONANDORGANIZATION");
            IfcIdTypeLookup.Add(12600, "IFCPHYSICALCOMPLEXQUANTITY");
            IfcIdTypeLookup.Add(12650, "IFCPILE");
            IfcIdTypeLookup.Add(12700, "IFCPIPEFITTINGTYPE");
            IfcIdTypeLookup.Add(12750, "IFCPIPESEGMENTTYPE");
            IfcIdTypeLookup.Add(12800, "IFCPLANAREXTENT");
            IfcIdTypeLookup.Add(12850, "IFCPLANE");
            IfcIdTypeLookup.Add(12900, "IFCPLATE");
            IfcIdTypeLookup.Add(12950, "IFCPLATETYPE");
            IfcIdTypeLookup.Add(13000, "IFCPOINTONCURVE");
            IfcIdTypeLookup.Add(13050, "IFCPOINTONSURFACE");
            IfcIdTypeLookup.Add(13100, "IFCPOLYGONALBOUNDEDHALFSPACE");
            IfcIdTypeLookup.Add(13150, "IFCPOLYLINE");
            IfcIdTypeLookup.Add(13200, "IFCPOLYLOOP");
            IfcIdTypeLookup.Add(13250, "IFCPOSTALADDRESS");
            IfcIdTypeLookup.Add(13300, "IFCPREDEFINEDSYMBOL");
            IfcIdTypeLookup.Add(13350, "IFCPRESENTATIONLAYERASSIGNMENT");
            IfcIdTypeLookup.Add(13400, "IFCPRESENTATIONLAYERWITHSTYLE");
            IfcIdTypeLookup.Add(13450, "IFCPRESENTATIONSTYLEASSIGNMENT");
            IfcIdTypeLookup.Add(13500, "IFCPRODUCTDEFINITIONSHAPE");
            IfcIdTypeLookup.Add(13550, "IFCPRODUCTREPRESENTATION");
            IfcIdTypeLookup.Add(13600, "IFCPROJECT");
            IfcIdTypeLookup.Add(13650, "IFCPROJECTIONELEMENT");
            IfcIdTypeLookup.Add(13700, "IFCPROPERTYBOUNDEDVALUE");
            IfcIdTypeLookup.Add(13750, "IFCPROPERTYCONSTRAINTRELATIONSHIP");
            IfcIdTypeLookup.Add(13800, "IFCPROPERTYDEPENDENCYRELATIONSHIP");
            IfcIdTypeLookup.Add(13850, "IFCPROPERTYENUMERATEDVALUE");
            IfcIdTypeLookup.Add(13900, "IFCPROPERTYENUMERATION");
            IfcIdTypeLookup.Add(13950, "IFCPROPERTYLISTVALUE");
            IfcIdTypeLookup.Add(14000, "IFCPROPERTYREFERENCEVALUE");
            IfcIdTypeLookup.Add(14050, "IFCPROPERTYSET");
            IfcIdTypeLookup.Add(14100, "IFCPROPERTYSINGLEVALUE");
            IfcIdTypeLookup.Add(14150, "IFCPROPERTYTABLEVALUE");
            IfcIdTypeLookup.Add(14200, "IFCPROTECTIVEDEVICETYPE");
            IfcIdTypeLookup.Add(14250, "IFCPROXY");
            IfcIdTypeLookup.Add(14300, "IFCPUMPTYPE");
            IfcIdTypeLookup.Add(14350, "IFCQUANTITYAREA");
            IfcIdTypeLookup.Add(14400, "IFCQUANTITYCOUNT");
            IfcIdTypeLookup.Add(14450, "IFCQUANTITYLENGTH");
            IfcIdTypeLookup.Add(14500, "IFCQUANTITYTIME");
            IfcIdTypeLookup.Add(14550, "IFCQUANTITYVOLUME");
            IfcIdTypeLookup.Add(14600, "IFCQUANTITYWEIGHT");
            IfcIdTypeLookup.Add(14650, "IFCRAILING");
            IfcIdTypeLookup.Add(14700, "IFCRAILINGTYPE");
            IfcIdTypeLookup.Add(14750, "IFCRAMP");
            IfcIdTypeLookup.Add(14800, "IFCRAMPFLIGHT");
            IfcIdTypeLookup.Add(14850, "IFCRAMPFLIGHTTYPE");
            IfcIdTypeLookup.Add(14900, "IFCRATIONALBEZIERCURVE");
            IfcIdTypeLookup.Add(14950, "IFCRECTANGLEHOLLOWPROFILEDEF");
            IfcIdTypeLookup.Add(15000, "IFCRECTANGLEPROFILEDEF");
            IfcIdTypeLookup.Add(15050, "IFCRECTANGULARTRIMMEDSURFACE");
            IfcIdTypeLookup.Add(15100, "IFCREFERENCESVALUEDOCUMENT");
            IfcIdTypeLookup.Add(15150, "IFCREGULARTIMESERIES");
            IfcIdTypeLookup.Add(15200, "IFCREINFORCEMENTBARPROPERTIES");
            IfcIdTypeLookup.Add(15250, "IFCREINFORCEMENTDEFINITIONPROPERTIES");
            IfcIdTypeLookup.Add(15300, "IFCREINFORCINGBAR");
            IfcIdTypeLookup.Add(15350, "IFCREINFORCINGMESH");
            IfcIdTypeLookup.Add(15400, "IFCRELAGGREGATES");
            IfcIdTypeLookup.Add(15450, "IFCRELASSIGNSTOACTOR");
            IfcIdTypeLookup.Add(15500, "IFCRELASSIGNSTOCONTROL");
            IfcIdTypeLookup.Add(15550, "IFCRELASSIGNSTOGROUP");
            IfcIdTypeLookup.Add(15600, "IFCRELASSIGNSTOPROCESS");
            IfcIdTypeLookup.Add(15650, "IFCRELASSIGNSTOPRODUCT");
            IfcIdTypeLookup.Add(15700, "IFCRELASSIGNSTORESOURCE");
            IfcIdTypeLookup.Add(15750, "IFCRELASSOCIATESAPPROVAL");
            IfcIdTypeLookup.Add(15800, "IFCRELASSOCIATESCLASSIFICATION");
            IfcIdTypeLookup.Add(15850, "IFCRELASSOCIATESDOCUMENT");
            IfcIdTypeLookup.Add(15900, "IFCRELASSOCIATESLIBRARY");
            IfcIdTypeLookup.Add(15950, "IFCRELASSOCIATESMATERIAL");
            IfcIdTypeLookup.Add(16000, "IFCRELASSOCIATESPROFILEPROPERTIES");
            IfcIdTypeLookup.Add(16050, "IFCRELCONNECTSELEMENTS");
            IfcIdTypeLookup.Add(16100, "IFCRELCONNECTSPATHELEMENTS");
            IfcIdTypeLookup.Add(16150, "IFCRELCONNECTSPORTS");
            IfcIdTypeLookup.Add(16200, "IFCRELCONNECTSPORTTOELEMENT");
            IfcIdTypeLookup.Add(16250, "IFCRELCONNECTSSTRUCTURALACTIVITY");
            IfcIdTypeLookup.Add(16300, "IFCRELCONNECTSSTRUCTURALELEMENT");
            IfcIdTypeLookup.Add(16350, "IFCRELCONNECTSSTRUCTURALMEMBER");
            IfcIdTypeLookup.Add(16400, "IFCRELCONNECTSWITHECCENTRICITY");
            IfcIdTypeLookup.Add(16450, "IFCRELCONNECTSWITHREALIZINGELEMENTS");
            IfcIdTypeLookup.Add(16500, "IFCRELCONTAINEDINSPATIALSTRUCTURE");
            IfcIdTypeLookup.Add(16550, "IFCRELCOVERSBLDGELEMENTS");
            IfcIdTypeLookup.Add(16600, "IFCRELCOVERSSPACES");
            IfcIdTypeLookup.Add(16650, "IFCRELDEFINESBYPROPERTIES");
            IfcIdTypeLookup.Add(16700, "IFCRELDEFINESBYTYPE");
            IfcIdTypeLookup.Add(16750, "IFCRELFILLSELEMENT");
            IfcIdTypeLookup.Add(16800, "IFCRELFLOWCONTROLELEMENTS");
            IfcIdTypeLookup.Add(16850, "IFCRELNESTS");
            IfcIdTypeLookup.Add(16900, "IFCRELOVERRIDESPROPERTIES");
            IfcIdTypeLookup.Add(16950, "IFCRELPROJECTSELEMENT");
            IfcIdTypeLookup.Add(17000, "IFCRELREFERENCEDINSPATIALSTRUCTURE");
            IfcIdTypeLookup.Add(17050, "IFCRELSEQUENCE");
            IfcIdTypeLookup.Add(17100, "IFCRELSERVICESBUILDINGS");
            IfcIdTypeLookup.Add(17150, "IFCRELSPACEBOUNDARY");
            IfcIdTypeLookup.Add(17200, "IFCRELVOIDSELEMENT");
            IfcIdTypeLookup.Add(17250, "IFCREPRESENTATION");
            IfcIdTypeLookup.Add(17300, "IFCREPRESENTATIONCONTEXT");
            IfcIdTypeLookup.Add(17350, "IFCREPRESENTATIONMAP");
            IfcIdTypeLookup.Add(17400, "IFCRESOURCEAPPROVALRELATIONSHIP");
            IfcIdTypeLookup.Add(17450, "IFCREVOLVEDAREASOLID");
            IfcIdTypeLookup.Add(17500, "IFCROOF");
            IfcIdTypeLookup.Add(17550, "IFCROUNDEDRECTANGLEPROFILEDEF");
            IfcIdTypeLookup.Add(17600, "IFCSANITARYTERMINALTYPE");
            IfcIdTypeLookup.Add(17650, "IFCSECTIONEDSPINE");
            IfcIdTypeLookup.Add(17700, "IFCSECTIONPROPERTIES");
            IfcIdTypeLookup.Add(17750, "IFCSECTIONREINFORCEMENTPROPERTIES");
            IfcIdTypeLookup.Add(17800, "IFCSENSORTYPE");
            IfcIdTypeLookup.Add(17850, "IFCSHAPEASPECT");
            IfcIdTypeLookup.Add(17900, "IFCSHAPEREPRESENTATION");
            IfcIdTypeLookup.Add(17950, "IFCSHELLBASEDSURFACEMODEL");
            IfcIdTypeLookup.Add(18000, "IFCSITE");
            IfcIdTypeLookup.Add(18050, "IFCSIUNIT");
            IfcIdTypeLookup.Add(18100, "IFCSLAB");
            IfcIdTypeLookup.Add(18150, "IFCSLABTYPE");
            IfcIdTypeLookup.Add(18200, "IFCSLIPPAGECONNECTIONCONDITION");
            IfcIdTypeLookup.Add(18250, "IFCSOUNDPROPERTIES");
            IfcIdTypeLookup.Add(18300, "IFCSOUNDVALUE");
            IfcIdTypeLookup.Add(18350, "IFCSPACE");
            IfcIdTypeLookup.Add(18400, "IFCSPACEHEATERTYPE");
            IfcIdTypeLookup.Add(18450, "IFCSPACETHERMALLOADPROPERTIES");
            IfcIdTypeLookup.Add(18500, "IFCSPACETYPE");
            IfcIdTypeLookup.Add(18550, "IFCSTACKTERMINALTYPE");
            IfcIdTypeLookup.Add(18600, "IFCSTAIR");
            IfcIdTypeLookup.Add(18650, "IFCSTAIRFLIGHT");
            IfcIdTypeLookup.Add(18700, "IFCSTAIRFLIGHTTYPE");
            IfcIdTypeLookup.Add(18750, "IFCSTRUCTURALANALYSISMODEL");
            IfcIdTypeLookup.Add(18800, "IFCSTRUCTURALCURVECONNECTION");
            IfcIdTypeLookup.Add(18850, "IFCSTRUCTURALCURVEMEMBER");
            IfcIdTypeLookup.Add(18900, "IFCSTRUCTURALCURVEMEMBERVARYING");
            IfcIdTypeLookup.Add(18950, "IFCSTRUCTURALLINEARACTION");
            IfcIdTypeLookup.Add(19000, "IFCSTRUCTURALLINEARACTIONVARYING");
            IfcIdTypeLookup.Add(19050, "IFCSTRUCTURALLOADGROUP");
            IfcIdTypeLookup.Add(19100, "IFCSTRUCTURALLOADLINEARFORCE");
            IfcIdTypeLookup.Add(19150, "IFCSTRUCTURALLOADPLANARFORCE");
            IfcIdTypeLookup.Add(19200, "IFCSTRUCTURALLOADSINGLEDISPLACEMENT");
            IfcIdTypeLookup.Add(19250, "IFCSTRUCTURALLOADSINGLEDISPLACEMENTDISTORTION");
            IfcIdTypeLookup.Add(19300, "IFCSTRUCTURALLOADSINGLEFORCE");
            IfcIdTypeLookup.Add(19350, "IFCSTRUCTURALLOADSINGLEFORCEWARPING");
            IfcIdTypeLookup.Add(19400, "IFCSTRUCTURALLOADTEMPERATURE");
            IfcIdTypeLookup.Add(19450, "IFCSTRUCTURALPLANARACTION");
            IfcIdTypeLookup.Add(19500, "IFCSTRUCTURALPLANARACTIONVARYING");
            IfcIdTypeLookup.Add(19550, "IFCSTRUCTURALPOINTACTION");
            IfcIdTypeLookup.Add(19600, "IFCSTRUCTURALPOINTCONNECTION");
            IfcIdTypeLookup.Add(19650, "IFCSTRUCTURALPOINTREACTION");
            IfcIdTypeLookup.Add(19700, "IFCSTRUCTURALPROFILEPROPERTIES");
            IfcIdTypeLookup.Add(19750, "IFCSTRUCTURALRESULTGROUP");
            IfcIdTypeLookup.Add(19800, "IFCSTRUCTURALSURFACECONNECTION");
            IfcIdTypeLookup.Add(19850, "IFCSTRUCTURALSURFACEMEMBER");
            IfcIdTypeLookup.Add(19900, "IFCSTRUCTURALSURFACEMEMBERVARYING");
            IfcIdTypeLookup.Add(19950, "IFCSTYLEDITEM");
            IfcIdTypeLookup.Add(20000, "IFCSTYLEDREPRESENTATION");
            IfcIdTypeLookup.Add(20050, "IFCSUBCONTRACTRESOURCE");
            IfcIdTypeLookup.Add(20100, "IFCSUBEDGE");
            IfcIdTypeLookup.Add(20150, "IFCSURFACECURVESWEPTAREASOLID");
            IfcIdTypeLookup.Add(20200, "IFCSURFACEOFLINEAREXTRUSION");
            IfcIdTypeLookup.Add(20250, "IFCSURFACEOFREVOLUTION");
            IfcIdTypeLookup.Add(20300, "IFCSURFACESTYLE");
            IfcIdTypeLookup.Add(20350, "IFCSURFACESTYLELIGHTING");
            IfcIdTypeLookup.Add(20400, "IFCSURFACESTYLEREFRACTION");
            IfcIdTypeLookup.Add(20450, "IFCSURFACESTYLERENDERING");
            IfcIdTypeLookup.Add(20500, "IFCSURFACESTYLESHADING");
            IfcIdTypeLookup.Add(20550, "IFCSURFACESTYLEWITHTEXTURES");
            IfcIdTypeLookup.Add(20600, "IFCSWEPTDISKSOLID");
            IfcIdTypeLookup.Add(20650, "IFCSWITCHINGDEVICETYPE");
            IfcIdTypeLookup.Add(20700, "IFCSYSTEM");
            IfcIdTypeLookup.Add(20750, "IFCSYSTEMFURNITUREELEMENTTYPE");
            IfcIdTypeLookup.Add(20800, "IFCTABLE");
            IfcIdTypeLookup.Add(20850, "IFCTABLEROW");
            IfcIdTypeLookup.Add(20900, "IFCTANKTYPE");
            IfcIdTypeLookup.Add(20950, "IFCTASK");
            IfcIdTypeLookup.Add(21000, "IFCTELECOMADDRESS");
            IfcIdTypeLookup.Add(21050, "IFCTENDON");
            IfcIdTypeLookup.Add(21100, "IFCTENDONANCHOR");
            IfcIdTypeLookup.Add(21150, "IFCTEXTLITERAL");
            IfcIdTypeLookup.Add(21200, "IFCTEXTLITERALWITHEXTENT");
            IfcIdTypeLookup.Add(21250, "IFCTEXTSTYLE");
            IfcIdTypeLookup.Add(21300, "IFCTEXTSTYLEFONTMODEL");
            IfcIdTypeLookup.Add(21350, "IFCTEXTSTYLEFORDEFINEDFONT");
            IfcIdTypeLookup.Add(21400, "IFCTEXTSTYLETEXTMODEL");
            IfcIdTypeLookup.Add(21450, "IFCTIMESERIESREFERENCERELATIONSHIP");
            IfcIdTypeLookup.Add(21500, "IFCTIMESERIESVALUE");
            IfcIdTypeLookup.Add(21550, "IFCTOPOLOGYREPRESENTATION");
            IfcIdTypeLookup.Add(21600, "IFCTRANSFORMERTYPE");
            IfcIdTypeLookup.Add(21650, "IFCTRANSPORTELEMENT");
            IfcIdTypeLookup.Add(21700, "IFCTRAPEZIUMPROFILEDEF");
            IfcIdTypeLookup.Add(21750, "IFCTRIMMEDCURVE");
            IfcIdTypeLookup.Add(21800, "IFCTSHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(21850, "IFCTUBEBUNDLETYPE");
            IfcIdTypeLookup.Add(21900, "IFCTWODIRECTIONREPEATFACTOR");
            IfcIdTypeLookup.Add(21950, "IFCTYPEOBJECT");
            IfcIdTypeLookup.Add(22000, "IFCTYPEPRODUCT");
            IfcIdTypeLookup.Add(22050, "IFCUNITARYEQUIPMENTTYPE");
            IfcIdTypeLookup.Add(22100, "IFCUNITASSIGNMENT");
            IfcIdTypeLookup.Add(22150, "IFCUSHAPEPROFILEDEF");
            IfcIdTypeLookup.Add(22200, "IFCVALVETYPE");
            IfcIdTypeLookup.Add(22250, "IFCVECTOR");
            IfcIdTypeLookup.Add(22300, "IFCVERTEX");
            IfcIdTypeLookup.Add(22350, "IFCVERTEXLOOP");
            IfcIdTypeLookup.Add(22400, "IFCVERTEXPOINT");
            IfcIdTypeLookup.Add(22450, "IFCVIBRATIONISOLATORTYPE");
            IfcIdTypeLookup.Add(22500, "IFCVIRTUALELEMENT");
            IfcIdTypeLookup.Add(22550, "IFCVIRTUALGRIDINTERSECTION");
            IfcIdTypeLookup.Add(22600, "IFCWALL");
            IfcIdTypeLookup.Add(22650, "IFCWALLSTANDARDCASE");
            IfcIdTypeLookup.Add(22700, "IFCWALLTYPE");
            IfcIdTypeLookup.Add(22750, "IFCWASTETERMINALTYPE");
            IfcIdTypeLookup.Add(22800, "IFCWINDOW");
            IfcIdTypeLookup.Add(22850, "IFCWINDOWLININGPROPERTIES");
            IfcIdTypeLookup.Add(22900, "IFCWINDOWPANELPROPERTIES");
            IfcIdTypeLookup.Add(22950, "IFCWINDOWSTYLE");
            IfcIdTypeLookup.Add(23000, "IFCZONE");
            IfcIdTypeLookup.Add(23050, "IFCZSHAPEPROFILEDEF");
            
        }

        private static Dictionary<string, IfcType> _IfcTypeLookup;

        public static Dictionary<string, IfcType> IfcTypeLookup
        {
            get { return _IfcTypeLookup; }
            
        }

        public static Dictionary<short, string> IfcIdTypeLookup
        {
            get { return _IfcIdTypeLookup; }
            
        }
        internal static void AddProperties(IfcType ifcType)
        {
            PropertyInfo[] properties =
                ifcType.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            foreach (PropertyInfo propInfo in properties)
            {
                IfcAttribute[] ifcAttributes =
                    (IfcAttribute[]) propInfo.GetCustomAttributes(typeof (IfcAttribute), false);
                if (ifcAttributes.GetLength(0) > 0) //we have an ifc property
                {
                    if (ifcAttributes[0].Order > 0)
                        ifcType.IfcProperties.Add(ifcAttributes[0].Order,
                                                  new IfcMetaProperty
                                                      {PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0]});
                    else
                        ifcType.IfcInverses.Add(new IfcMetaProperty
                                                    {PropertyInfo = propInfo, IfcAttribute = ifcAttributes[0]});
                }
                IfcPrimaryIndex[] ifcPrimaryIndices =
                    (IfcPrimaryIndex[]) propInfo.GetCustomAttributes(typeof (IfcPrimaryIndex), false);
                if (ifcPrimaryIndices.GetLength(0) > 0) //we have an ifc primary index
                    ifcType.PrimaryIndex = propInfo;
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