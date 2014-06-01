/** @jsx React.DOM */ 

var React = require('react');
var ReactPropTypes = React.PropTypes;
var SearchTextInput = require('./SearchTextInput.react');
var User = require('./User.react');
var AppActions = require('../actions/AppActions');
var UserStore = require('../stores/UserStore');

var NavigationBar = React.createClass({

    getInitialState: function(){
	return { user: UserStore.getUser() };
    },

    componentDidMount: function() {
	UserStore.addChangeListener(this._onChange);
    },

    componentWillUnmount: function() {
	UserStore.removeChangeListener(this._onChange);
    },

    onSubmit: function(value) {
	AppActions.search(value);
    },

    render: function() {
	return (
      	    <div className="navbar navbar-default navbar-fixed-top" role="navigation">
		<div className="container">
	
		<div className="row">
  		    <div className="navbar-header">
		       <a className="navbar-brand" href="#">Deal Hunter</a>
		    </div>
		    <div className="collapse navbar-collapse">

               	       <div className="col-sm-6 col-md-6">
	                  <SearchTextInput onSubmit={this.onSubmit} />
	               </div>

                       <div className="col-sm-2 col-md-2">
	                 <ul className="nav navbar-nav">
		           <li className="active"><a href="#">Deals</a></li>
                         </ul>
	               </div>
	    
	               <ul className="nav navbar-nav navbar-right">
		         <User user={this.state.user} />
	               </ul>

	            </div>
	         </div>
              </div>
           </div>
	);
    },

    _onChange: function() {
	this.setState({user: UserStore.getUser()});
    }
});

module.exports = NavigationBar;
