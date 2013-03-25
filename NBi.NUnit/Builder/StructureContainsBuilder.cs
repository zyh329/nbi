﻿using System;
using System.Linq;
using NBi.Core.Analysis.Metadata;
using NBi.Core.Analysis.Request;
using NBi.Xml.Constraints;
using NBi.Xml.Items;
using NBi.Xml.Systems;

namespace NBi.NUnit.Builder
{
    class StructureContainsBuilder: AbstractStructureBuilder
    {
        protected ContainsXml ConstraintXml {get; set;}
        
        public StructureContainsBuilder() : base()
        {
        }

        internal StructureContainsBuilder(DiscoveryRequestFactory factory)
            : base(factory)
        {
        }

        protected override void SpecificSetup(AbstractSystemUnderTestXml sutXml, AbstractConstraintXml ctrXml)
        {
            if (!(ctrXml is ContainsXml))
                throw new ArgumentException("Constraint must be a 'ContainsXml'");

            ConstraintXml = (ContainsXml)ctrXml;
        }

        protected override void SpecificBuild()
        {
            Constraint = InstantiateConstraint(ConstraintXml);
        }

        protected global::NUnit.Framework.Constraints.Constraint InstantiateConstraint(ContainsXml ctrXml)
        {
            global::NUnit.Framework.Constraints.Constraint ctr = null;

            if (ctrXml.Items.Count==1)
                ctr = new NBi.NUnit.Structure.CollectionItemConstraint(ctrXml.Caption);
            else
                ctr = new NBi.NUnit.Structure.EquivalentToConstraint(ctrXml.Items);

            if (ctr is NBi.NUnit.Structure.CollectionItemConstraint)
            {
                //Ignore-case if requested
                if (ctrXml.IgnoreCase)
                    ctr = ((NBi.NUnit.Structure.CollectionItemConstraint)ctr).IgnoreCase;
            }

            if (ctr is NBi.NUnit.Structure.EquivalentToConstraint)
            {
                //Ignore-case if requested
                if (ctrXml.IgnoreCase)
                    ctr = ((NBi.NUnit.Structure.EquivalentToConstraint)ctr).IgnoreCase;
            }
            return ctr;
        }

        protected override object InstantiateSystemUnderTest(StructureXml sutXml)
        {
            string perspective = null, measuregroup = null, displayFolder = null, measure = null, dimension = null, hierarchy = null, level = null;

            if (string.IsNullOrEmpty(sutXml.Item.Caption))
                throw new ArgumentException("Caption must be specified");

            if (sutXml.Item.GetType()==typeof(PerspectiveXml))
            {
                throw new ArgumentException();
            }
            if (sutXml.Item is MeasureGroupXml)
            {
                var persp = ((MeasureGroupXml)sutXml.Item).Perspective;
                if (string.IsNullOrEmpty(persp))
                    throw new ArgumentException("Perspective must be specified");

                perspective = ((MeasureGroupXml)sutXml.Item).Perspective;
                measuregroup = sutXml.Item.Caption;
            }
            if (sutXml.Item.GetType() == typeof(MeasureXml))
            {
                throw new ArgumentException();
            }
            if (sutXml.Item is DimensionXml)
            {
                var persp = ((DimensionXml)sutXml.Item).Perspective;
                if (string.IsNullOrEmpty(persp))
                    throw new ArgumentException("Perspective must be specified");

                perspective = persp;
                dimension = sutXml.Item.Caption;
            }
            if (sutXml.Item is HierarchyXml)
            {
                var dim = ((HierarchyXml)sutXml.Item).Dimension;
                if (string.IsNullOrEmpty(dim))
                    throw new ArgumentException("Perspective must be specified");

                dimension = dim;
                hierarchy = sutXml.Item.Caption;
            }
            if (sutXml.Item is LevelXml)
            {
                throw new ArgumentException();
            }

            var target = GetTarget(sutXml.Item);

            return discoveryFactory.Build
                (
                    sutXml.GetConnectionString(),
                    target,
                    perspective,
                    measuregroup,
                    displayFolder,
                    measure,
                    dimension,
                    hierarchy,
                    level
                );
        }

        private DiscoveryTarget GetTarget(AbstractItem item)
        {

            if (item is MeasureGroupXml)
                return DiscoveryTarget.Measures;
            if (item is HierarchyXml)
                return DiscoveryTarget.Levels;
            if (item is DimensionXml)
                return DiscoveryTarget.Hierarchies;

            throw new ArgumentException();
            
        }

    }
}
