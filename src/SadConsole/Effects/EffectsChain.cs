﻿namespace SadConsole.Effects
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Chains one effect after another.
    /// </summary>
    [DataContract]
    public class EffectsChain : CellEffectBase
    {
        [DataMember]
        private List<Cell> _addedCells = new List<Cell>();

        [DataMember]
        public List<ICellEffect> Effects = new List<ICellEffect>();
        [DataMember]
        public bool Repeat;

        [DataMember]
        private bool _enabled = false;
        [DataMember]
        private ICellEffect _activeEffect;
        [DataMember]
        private int _activeIndex = -1;

        [DataMember]
        private bool _inChainDelay = false;

        [DataMember]
        public double DelayBetweenEffects = 0d;

        public void Start()
        {
            if (Effects.Count > 0)
            {
                _activeIndex = 0;
                _activeEffect = Effects[0];
                _enabled = true;
            }
            else
                _enabled = false;
        }

        public void End()
        {
            _enabled = false;
        }

        public override void AddCell(Cell cell)
        {
            _addedCells.Add(cell);
            _activeEffect?.AddCell(cell);
        }

        public override void ClearCell(Cell cell)
        {
            _addedCells.Remove(cell);
            _activeEffect?.ClearCell(cell);
        }

        public override bool UpdateCell(Cell cell)
        {
            if (_activeEffect != null)
                return _activeEffect.UpdateCell(cell);
            else
                return false;
        }

        public override void Update(double gameTimeSeconds)
        {
            if (_enabled)
            {
                base.Update(gameTimeSeconds);

                if (_delayFinished)
                {
                    // Check to see if we are in a chain delay, if so, we wait x seconds until we move on to the next chained effect.
                    if (!_inChainDelay)
                    {
                        // Process the effect
                        _activeEffect.Update(gameTimeSeconds);

                        // If the effect finished, we move on to the next effect
                        if (_activeEffect.IsFinished)
                        {
                            foreach (var cell in _addedCells)
                                _activeEffect.ClearCell(cell);

                            _activeIndex++;

                            if (_activeIndex != Effects.Count)
                            {
                                _activeEffect = Effects[_activeIndex];

                                foreach (var cell in _addedCells)
                                    _activeEffect.AddCell(cell);

                                // When moving to the next effect, check and see if we have a delay. If so, flag and wait.
                                if (DelayBetweenEffects != 0f)
                                {
                                    _inChainDelay = true;
                                    _timeElapsed = 0d;
                                }
                            }
                            else
                            {
                                _activeIndex = -1;
                                _activeEffect = null;

                                IsFinished = true;
                            }
                        }

                        // No effect to process? End this chain, which sets enabled = false.
                        if (_activeEffect == null)
                        {
                            if (!Repeat)
                                End();
                            else
                            {
                                _inChainDelay = true;
                                _timeElapsed = 0d;
                                IsFinished = false;
                            }
                        }
                    }
                    else
                    {
                        if (_timeElapsed >= DelayBetweenEffects)
                        {
                            _inChainDelay = false;

                            // If we do not have another effect to move on to and repeat is enabled, start over.
                            // We can only do this if the effects have ended with repeat, otherwise End() is called which instantly disables the chain
                            if (_activeEffect == null && Repeat)
                                Restart();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Restarts the cell effect but does not reset it.
        /// </summary>
        public override void Restart()
        {
            base.Restart();

            _inChainDelay = false;

            foreach (var item in Effects)
                item.Restart();

            Start();

            base.Restart();
        }

        public override ICellEffect Clone()
        {
            EffectsChain chain = new EffectsChain()
            {
                _enabled = this._enabled,
                _activeIndex = this._activeIndex,
                DelayBetweenEffects = this.DelayBetweenEffects,
                Repeat = this.Repeat,

                IsFinished = IsFinished,
                StartDelay = StartDelay,
                CloneOnApply = CloneOnApply,
                RemoveOnFinished = RemoveOnFinished,
                DiscardCellState = DiscardCellState,
                Permanent = Permanent,
                _timeElapsed = _timeElapsed,
            };

            foreach (var item in Effects)
                chain.Effects.Add(item.CloneOnApply ? item.Clone() : item);

            chain._activeEffect = chain.Effects[chain._activeIndex];
            return chain;
        }

        //public override bool Equals(ICellEffect effect)
        //{
        //    if (effect is EffectsChain)
        //    {
        //        if (base.Equals(effect))
        //        {
        //            var effect2 = (EffectsChain)effect;

        //            return StartDelay == effect2.StartDelay &&
        //                   RemoveOnFinished == effect2.RemoveOnFinished &&
        //                   DelayBetweenEffects == effect2.DelayBetweenEffects &&
        //                   Repeat == effect2.Repeat;
        //        }
        //    }
                
        //    return false;
        //}

        public override string ToString()
        {
            return string.Format("EFFECTSCHAIN-{0}-{1}-{2}-{3}", StartDelay, RemoveOnFinished, DelayBetweenEffects, Repeat);
        }

        [OnDeserializedAttribute]
        private void AfterDeserialized(StreamingContext context)
        {
            if (_enabled)
                Start();
        }
    }
}
