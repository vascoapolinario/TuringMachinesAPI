
import React from 'react';

interface CardProps {
  title?: string;
  subtitle?: string;
  children: React.ReactNode;
  className?: string;
  action?: React.ReactNode;
  noPadding?: boolean;
}

export const Card: React.FC<CardProps> = ({ title, subtitle, children, className = "", action, noPadding = false }) => {
  return (
    <div className={`modern-card rounded-xl overflow-hidden shadow-sm flex flex-col ${className}`}>
      {(title || action) && (
        <div className="px-6 py-5 border-b border-slate-800 flex justify-between items-start shrink-0">
          <div>
            {title && <h3 className="text-base font-semibold text-slate-100">{title}</h3>}
            {subtitle && <p className="text-xs text-slate-500 mt-1">{subtitle}</p>}
          </div>
          {action && <div>{action}</div>}
        </div>
      )}
      <div className={`flex-1 flex flex-col min-h-0 ${noPadding ? '' : 'p-6'}`}>
        {children}
      </div>
    </div>
  );
};
